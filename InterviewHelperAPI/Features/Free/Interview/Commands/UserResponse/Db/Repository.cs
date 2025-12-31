using System.Text.Json;
using InterviewHelperAPI.Service.GigaChat;
using Microsoft.EntityFrameworkCore;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse.Db;

public class Repository : IRepository
{
    private readonly IGigaChatService _gigaChatService;
    private readonly HelperDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IInterviewSessionManager  _sessionManager;

    public Repository(
        IGigaChatService gigaChatService,
        HelperDbContext db,
        IHttpContextAccessor httpContextAccessor,
        IInterviewSessionManager  sessionManager)
    {
        _gigaChatService = gigaChatService;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _sessionManager = sessionManager;
    }
    
    
    public async Task<InterviewContext> RestoreSessionFromDatabase(int interviewId)
    {
        var interview = await _db.Interviews.FirstOrDefaultAsync(i => i.InterviewId == interviewId);

        if (interview == null)
            throw new Exception($"Интерьвю {interviewId} не найдено");

        var context = new InterviewContext
        {
            InterviewId = interview.InterviewId,
            UserId = interview.UserId,
            JobTitle = interview.JobTitle,
            JobLevel = interview.JobLevel ?? "middle",
            JobDescription = interview.JobDescription,
            StartedAt = interview.StartedAt ?? DateTime.UtcNow
        };

        var questionsAndAnswers = await _db.InterviewQuestions
            .Where(q => q.InterviewId == interviewId)
            .OrderBy(q => q.AskedAt)
            .Select(q => new
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                AskedAt = q.AskedAt,
                Response = _db.UserResponses
                    .FirstOrDefault(ur => ur.QuestionId == q.QuestionId)
            })
            .ToListAsync();

        foreach (var item in questionsAndAnswers)
        {
            context.AddMessage("assistant", item.QuestionText);
            
            if (item.Response != null)
            {
                context.AddMessage("user", item.Response.UserAnswer);
            }
        }

        var skillEvaluations = await _db.SkillEvaluations
            .Where(se => se.InterviewId == interviewId && !se.IsFinal.HasValue)
            .ToListAsync();

        foreach (var skill in skillEvaluations)
        {
            context.SkillEvaluations.Add(new Service.GigaChat.SkillEvaluation
            {
                SkillName = skill.SkillName,
                Score = skill.Score,
                Evidence = skill.Evidence ?? string.Empty,
                EvaluatedAt = skill.EvaluatedAt ?? DateTime.UtcNow
            });
        }

        return context;
    }
    
    public async Task CompleteInterview(InterviewHelperAPI.Interview interview, InterviewContext context, decimal finalScore)
    {
        var summary = await _gigaChatService.GenerateSummaryAsync(context);
        
        interview.Status = "completed";
        interview.CompletedAt = DateTime.UtcNow;
        interview.TotalScore = finalScore;
        interview.AiFeedbackSummary = summary.OverallFeedback;
        interview.Recommendations = string.Join("; ", summary.RecommendedTopics.Select(rt => rt.Topic));
        interview.DurationSeconds = (int)context.GetDuration().TotalSeconds;

        foreach (var skill in context.SkillEvaluations)
        {
            var dbSkill = new InterviewHelperAPI.SkillEvaluation
            {
                InterviewId = interview.InterviewId,
                SkillName = skill.SkillName,
                SkillCategory = GetSkillCategory(skill.SkillName),
                Score = skill.Score,
                Evidence = skill.Evidence,
                IsFinal = true,
                EvaluatedAt = DateTime.UtcNow
            };
            await _db.SkillEvaluations.AddAsync(dbSkill);
        }

        foreach (var topic in summary.RecommendedTopics)
        {
            var studyTopic = new StudyTopic
            {
                InterviewId = interview.InterviewId,
                SkillName = topic.Topic,
                TopicName = topic.Topic,
                Priority = topic.Priority,
                Reason = $"Рекомендовано по итогам собеседования. Оценка: {finalScore:F1}",
                Resources = JsonSerializer.Serialize(topic.Resources),
                AddedAt = DateTime.UtcNow
            };
            await _db.StudyTopics.AddAsync(studyTopic);
        }

        await _sessionManager.CompleteSessionAsync(interview.InterviewId);
    }

    public async Task SaveSkillEvaluations(int interviewId, AnswerEvaluation evaluation, InterviewContext context)
    {
        foreach (var skill in evaluation.DetectedSkills)
        {
            var existingSkill = await _db.SkillEvaluations
                .FirstOrDefaultAsync(se => 
                    se.InterviewId == interviewId && 
                    se.SkillName == skill && 
                    !se.IsFinal.HasValue);

            if (existingSkill != null)
            {
                existingSkill.Score = (existingSkill.Score + evaluation.Score) / 2;
                existingSkill.Evidence += $"; {evaluation.Feedback}";
                existingSkill.EvaluatedAt = DateTime.UtcNow;
                _db.SkillEvaluations.Update(existingSkill);
            }
            else
            {
                var skillEvaluation = new InterviewHelperAPI.SkillEvaluation
                {
                    InterviewId = interviewId,
                    SkillName = skill,
                    SkillCategory = GetSkillCategory(skill),
                    Score = evaluation.Score,
                    Evidence = evaluation.Feedback,
                    IsFinal = false,
                    EvaluatedAt = DateTime.UtcNow
                };
                await _db.SkillEvaluations.AddAsync(skillEvaluation);
            }
        }
    }
    
    public async Task UpdateUserStatistics(int userId, decimal score)
    {
        var statistic = await _db.UserStatistics
            .FirstOrDefaultAsync(us => us.UserId == userId);

        if (statistic == null)
        {
            statistic = new UserStatistic
            {
                UserId = userId,
                TotalInterviews = 1,
                CompletedInterviews = 1,
                AvgTotalScore = score,
                BestScore = score,
                LastInterviewDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _db.UserStatistics.AddAsync(statistic);
        }
        else
        {
            statistic.TotalInterviews = (statistic.TotalInterviews ?? 0) + 1;
            statistic.CompletedInterviews = (statistic.CompletedInterviews ?? 0) + 1;
            
            var totalScore = (statistic.AvgTotalScore ?? 0) * (statistic.CompletedInterviews ?? 1 - 1) + score;
            statistic.AvgTotalScore = totalScore / (statistic.CompletedInterviews ?? 1);
            
            if (score > (statistic.BestScore ?? 0))
            {
                statistic.BestScore = score;
            }
            
            statistic.LastInterviewDate = DateTime.UtcNow;
            statistic.UpdatedAt = DateTime.UtcNow;
            _db.UserStatistics.Update(statistic);
        }
    }
    
    public string GetDifficultyBasedOnScore(decimal score)
    {
        return score switch
        {
            >= 8 => "hard",
            >= 5 => "medium",
            _ => "easy"
        };
    }

    public string GetSkillCategory(string skillName)
    {
        var lowerSkill = skillName.ToLower();
        
        if (lowerSkill.Contains("sql") || lowerSkill.Contains("database"))
            return "database";
        if (lowerSkill.Contains("коммуникация") || lowerSkill.Contains("общение"))
            return "soft_skills";
        if (lowerSkill.Contains("алгоритм") || lowerSkill.Contains("структура данных"))
            return "algorithms";
        if (lowerSkill.Contains("тестирование") || lowerSkill.Contains("qa"))
            return "testing";
            
        return "programming";
    }
}