using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace InterviewHelperAPI.Service.GigaChat;

public class GigaChatService : IGigaChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GigaChatSettings _settings;
    private readonly ILogger<GigaChatService> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiresAt;
    private readonly object _tokenLock = new object();

    public GigaChatService(
        IHttpClientFactory httpClientFactory,
        IOptions<GigaChatSettings> settings,
        ILogger<GigaChatService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateQuestionAsync(
        string jobDescription, 
        string jobTitle, 
        string jobLevel, 
        InterviewContext? context)
    {
        try
        {
            _logger.LogInformation("Генерация вопроса для {JobTitle} ({JobLevel})", jobTitle, jobLevel);
            
            await EnsureValidTokenAsync();
            
            var messages = BuildPromptForQuestion(jobDescription, jobTitle, jobLevel, context);
            var response = await SendChatRequestAsync(messages);
            
            if (context != null)
            {
                context.AddMessage("assistant", response);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации вопроса");
            //МОК вопрос
            if (_settings.ClientId.Contains("development") || string.IsNullOrEmpty(_settings.ClientId))
            {
                return $"Вопрос по {jobTitle} (уровень: {jobLevel}): Расскажите о вашем опыте работы с {jobDescription.Split(' ').FirstOrDefault() ?? "технологиями"}?";
            }
            
            throw new Exception($"Не удалось сгенерировать вопрос: {ex.Message}");
        }
    }

    public async Task<AnswerEvaluation> EvaluateAnswerAsync(
        string question, 
        string userAnswer, 
        InterviewContext context)
    {
        try
        {
            _logger.LogInformation("Оценка ответа на вопрос: {Question}", question.Substring(0, Math.Min(50, question.Length)));
            
            await EnsureValidTokenAsync();
            
            var messages = BuildPromptForEvaluation(question, userAnswer, context);
            var response = await SendChatRequestAsync(messages);
            
            var evaluation = ParseEvaluationResponse(response);
            
            context.AddMessage("user", userAnswer);
            
            UpdateSkillEvaluations(context, evaluation);
            
            return evaluation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при оценке ответа");
            throw new Exception($"Не удалось оценить ответ: {ex.Message}");
        }
    }

    public async Task<InterviewSummary> GenerateSummaryAsync(InterviewContext context)
    {
        try
        {
            _logger.LogInformation("Генерация итогов собеседования #{InterviewId}", context.InterviewId);

            await EnsureValidTokenAsync();

            var messages = BuildPromptForSummary(context);
            var response = await SendChatRequestAsync(messages);

            var summary = ParseSummaryResponse(response);
            summary.FinalScore = context.CalculateAverageScore();

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации итогов");
            throw new Exception($"Не удалось сгенерировать итоги: {ex.Message}");
        }
    }
    
    public async Task<string> GenerateFeedbackAsync(string question, string userAnswer, InterviewContext context)
{
    try
    {
        await EnsureValidTokenAsync();
        
        var messages = new List<GigaChatMessage>
        {
            new GigaChatMessage
            {
                Role = "system",
                Content = $"Ты - рекрутер. Дай развернутую обратную связь по ответу.\n\n" +
                         $"Вопрос: '{question}'\n" +
                         $"Ответ кандидата: '{userAnswer}'\n\n" +
                         "Структура фидбека:\n" +
                         "1. Что было хорошо\n" +
                         "2. Что можно улучшить\n" +
                         "3. Конкретные рекомендации\n\n" +
                         "Говори как человек, избегай шаблонных фраз. " +
                         "Используй местоимение 'вы'. Верни только фидбек."
            }
        };
        
        return await SendChatRequestAsync(messages);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка при генерации фидбека");
        return "Хорошо, давайте попробуем другой пример из вашего практического опыта.";
    }
}

    private async Task<string> SendChatRequestAsync(List<GigaChatMessage> messages)
    {
        var httpClient = _httpClientFactory.CreateClient("GigaChat");
        
        var requestBody = new
        {
            model = "GigaChat",
            messages = messages,
            temperature = _settings.Temperature,
            max_tokens = _settings.MaxTokens,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _accessToken);
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            _logger.LogDebug("Отправка запроса к GigaChat API");
            var response = await httpClient.PostAsync(_settings.ApiUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("GigaChat API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                throw new Exception($"GigaChat API error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GigaChatResponse>(responseJson);

            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? 
                   "Не удалось получить ответ от GigaChat";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ошибка HTTP при вызове GigaChat API");
            throw new Exception($"Ошибка подключения к GigaChat: {ex.Message}");
        }
    }

    private async Task EnsureValidTokenAsync()
    {
        lock (_tokenLock)
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt)
                return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("GigaChat");
            
            var authString = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, _settings.AuthUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
            request.Headers.Add("RqUID", Guid.NewGuid().ToString());
            request.Headers.Add("Accept", "application/json");

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", _settings.Scope)
            });
            request.Content = formContent;

            _logger.LogInformation("Получение токена доступа GigaChat");
            var response = await httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ошибка получения токена: {StatusCode} - {Error}", 
                    response.StatusCode, error);
                throw new Exception($"Ошибка авторизации GigaChat: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

            lock (_tokenLock)
            {
                _accessToken = tokenResponse?.AccessToken;
                var expiresAt = tokenResponse?.ExpiresAt ?? 0;
                
                if (expiresAt > 0)
                {
                    _tokenExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).UtcDateTime;
                }
                else
                {
                    _tokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
                }
            }
            
            _logger.LogInformation("Токен GigaChat получен, истекает в {ExpiresAt}", _tokenExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении токена GigaChat");
            throw new Exception($"Не удалось получить токен доступа: {ex.Message}");
        }
    }

    private List<GigaChatMessage> BuildPromptForQuestion(
        string jobDescription, 
        string jobTitle, 
        string jobLevel, 
        InterviewContext? context)
    {
        var messages = new List<GigaChatMessage>();
    
        string systemPrompt = $@"Ты - опытный рекрутер с 10+ годами опыта в IT.
    Проводишь собеседование на позицию: {jobTitle} ({jobLevel} уровень).
    
    Описание вакансии:
    {jobDescription}
    
    Правила:
    1. Задавай только ОДИН технический/поведенческий вопрос
    2. Вопрос должен быть релевантен вакансии и уровню
    3. Фокусируйся на практическом опыте, а не теории
    4. Адаптируй сложность на основе истории диалога
    5. Вопрос должен проверить конкретный навык или опыт
    6. Верни ТОЛЬКО вопрос, без пояснений
    
    Примеры хороших вопросов:
    - Как вы обычно деплоите FastAPI приложение в прод?
    - Расскажите о вашем опыте оптимизации SQL запросов
    - Как вы отлаживаете проблему с памятью в продакшене?
    - Опишите ваш опыт работы с микросервисной архитектурой
    ";
    
        messages.Add(new GigaChatMessage { Role = "system", Content = systemPrompt });

        // Добавляем историю диалога (последние 4 сообщения)
        if (context != null && context.ConversationHistory.Any())
        {
            var recentHistory = context.ConversationHistory
                .TakeLast(4)
                .Select(m => new GigaChatMessage
                {
                    Role = m.Role,
                    Content = m.Content
                });
        
            messages.AddRange(recentHistory);
        }

        // Добавляем промпт для генерации вопроса
        messages.Add(new GigaChatMessage 
        { 
            Role = "user", 
            Content = "Задай следующий вопрос для собеседования." 
        });

        return messages;
    }

    private List<GigaChatMessage> BuildPromptForEvaluation(
        string question, 
        string userAnswer, 
        InterviewContext context)
    {
        var historySummary = context.GetFormattedHistory();
    
        return new List<GigaChatMessage>
        {
            new GigaChatMessage
            {
                Role = "system",
                Content = $@"Ты - технический рекрутер. Оцени ответ кандидата.
            
            Контекст:
            - Должность: {context.JobTitle}
            - Уровень: {context.JobLevel}
            
            Критерии оценки (0-10):
            1. Техническая точность (0-3)
            2. Глубина понимания (0-3)
            3. Практический опыт (0-2)
            4. Четкость изложения (0-2)
            
            ВЕРНИ ТОЛЬКО JSON:
            {{
                ""score"": 0-10,
                ""feedback"": ""конструктивный фидбек"",
                ""detectedSkills"": [""навык1"", ""навык2""],
                ""improvementAreas"": [""область1"", ""область2""],
                ""nextQuestionHint"": ""подсказка в формате рекрутера, например: 'Можете рассказать о...' или 'Что вы думаете о...'"",
                ""detailedScores"": {{
                    ""technicalCorrectness"": 0-3,
                    ""depthOfUnderstanding"": 0-3,
                    ""practicalApplication"": 0-2,
                    ""answerStructure"": 0-2
                }}
            }}
            
            Вопрос: {question}
            Ответ: {userAnswer}"
            }
        };
    }

    private List<GigaChatMessage> BuildPromptForSummary(InterviewContext context)
    {
        var history = context.GetFormattedHistory();
        var averageScore = context.CalculateAverageScore();
    
        return new List<GigaChatMessage>
        {
            new GigaChatMessage
            {
                Role = "system",
                Content = $@"Ты - старший рекрутер. Подведи итоги собеседования.
            
            Контекст:
            - Должность: {context.JobTitle}
            - Уровень: {context.JobLevel}
            - Средний балл: {averageScore}/10
            
            ВЕРНИ ТОЛЬКО JSON:
            {{
                ""finalScore"": 0-10,
                ""strengths"": [""сильная сторона1"", ""сильная сторона2""],
                ""weaknesses"": [""слабая сторона1"", ""слабая сторона2""],
                ""overallFeedback"": ""общий фидбэк"",
                ""recommendedTopics"": [
                    {{
                        ""topic"": ""тема"",
                        ""priority"": ""high/medium/low"",
                        ""resources"": [""ресурс1"", ""ресурс2""]
                    }}
                ],
                ""suggestedLevel"": ""junior/middle/senior"",
                ""isRecommended"": true/false,
                ""interviewDuration"": ""примерно X минут""
            }}
            
            История: {history}"
            }
        };
    }

    public async Task<string> GenerateHintAsync(string question, InterviewContext context)
    {
        try
        {
            await EnsureValidTokenAsync();
        
            var messages = new List<GigaChatMessage>
            {
                new GigaChatMessage
                {
                    Role = "system",
                    Content = $"Ты - рекрутер. Дай небольшую подсказку кандидату для вопроса: '{question}'. " +
                              "Формат: 'Можете рассказать о...' или 'Что вы думаете о...' или 'Как вы обычно...'. " +
                              "Не давай полный ответ, только направление. " +
                              "Верни только подсказку, 1-2 предложения."
                }
            };
        
            return await SendChatRequestAsync(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации подсказки");
            return "Можете рассказать о вашем практическом опыте в этой области?";
        }
    }
    
    private AnswerEvaluation ParseEvaluationResponse(string response)
    {
        try
        {
            // Пытаемся найти JSON в ответе (на случай если AI добавил текст)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<AnswerEvaluation>(json, options) 
                       ?? new AnswerEvaluation { Feedback = response };
            }
            
            return JsonSerializer.Deserialize<AnswerEvaluation>(response) 
                   ?? new AnswerEvaluation { Feedback = response };
        }
        catch (JsonException)
        {
            // Fallback: если AI не вернул JSON
            return new AnswerEvaluation
            {
                Feedback = response,
                Score = 5,
                DetectedSkills = new List<string>(),
                ImprovementAreas = new List<string>(),
                NextQuestionHint = "Попробуйте углубиться в практические аспекты"
            };
        }
    }

    private InterviewSummary ParseSummaryResponse(string response)
    {
        try
        {
            // Пытаемся найти JSON в ответе
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return JsonSerializer.Deserialize<InterviewSummary>(json, options)
                       ?? new InterviewSummary { OverallFeedback = response };
            }
            
            return JsonSerializer.Deserialize<InterviewSummary>(response)
                   ?? new InterviewSummary { OverallFeedback = response };
        }
        catch (JsonException)
        {
            return new InterviewSummary
            {
                OverallFeedback = response,
                FinalScore = 5,
                Strengths = new List<string>(),
                Weaknesses = new List<string>(),
                RecommendedTopics = new List<RecommendedTopic>(),
                SuggestedLevel = "middle"
            };
        }
    }

    private void UpdateSkillEvaluations(InterviewContext context, AnswerEvaluation evaluation)
    {
        foreach (var skill in evaluation.DetectedSkills)
        {
            var existingSkill = context.SkillEvaluations
                .FirstOrDefault(s => s.SkillName.Equals(skill, StringComparison.OrdinalIgnoreCase));
            
            if (existingSkill != null)
            {
                // Обновляем оценку (усредняем)
                existingSkill.Score = (existingSkill.Score + evaluation.Score) / 2;
                existingSkill.Evidence += $"; {evaluation.Feedback}";
                existingSkill.EvaluatedAt = DateTime.UtcNow;
            }
            else
            {
                context.SkillEvaluations.Add(new SkillEvaluation
                {
                    SkillName = skill,
                    Score = evaluation.Score,
                    Evidence = evaluation.Feedback,
                    EvaluatedAt = DateTime.UtcNow
                });
            }
        }
    }
}

// Вспомогательные классы для десериализации
public class GigaChatResponse
{
    [JsonPropertyName("choices")]
    public List<GigaChatChoice> Choices { get; set; } = new();
    
    [JsonPropertyName("usage")]
    public TokenUsage Usage { get; set; } = new();
}

public class GigaChatChoice
{
    [JsonPropertyName("message")]
    public GigaChatMessage Message { get; set; } = new();
}

public class GigaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class TokenUsage
{
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    
    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
    
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}