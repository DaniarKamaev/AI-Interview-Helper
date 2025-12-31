using InterviewHelperAPI.Features.Auth;
namespace InterviewHelperAPI.Features.Free.Interview.Commands.Start;

using Service.GigaChat;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class StartInterviewHandler : IRequestHandler<StartInterviewCommand, StartInterviewResponse>
{
    private readonly IInterviewSessionManager _sessionManager;
    private readonly IGigaChatService _gigaChatService;
    private readonly HelperDbContext _dbContext;
    private readonly ILogger<StartInterviewHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public StartInterviewHandler(
        IInterviewSessionManager sessionManager,
        IGigaChatService gigaChatService,
        HelperDbContext dbContext,
        ILogger<StartInterviewHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _sessionManager = sessionManager;
        _gigaChatService = gigaChatService;
        _dbContext = dbContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    
    public async Task<StartInterviewResponse> Handle(
        StartInterviewCommand request, 
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            
            int userid = AuthHelper.GetCurrentUserId(_httpContextAccessor);
            
            _logger.LogInformation("Начало создания собеседования для пользователя {UserId}", request.UserId);
            
            var userExists = await _dbContext.Users
                .AnyAsync(u => u.UserId == userid, cancellationToken);
            
            if (!userExists)
            {
                throw new Exception($"Пользователь с ID {userid} не найден");
            }
            
            var interview = new InterviewHelperAPI.Interview
            {
                UserId = userid,
                JobTitle = request.JobTitle,
                JobDescription = request.JobDescription,
                JobLevel = request.JobLevel ?? "middle",
                Status = "in_progress",
                StartedAt = DateTime.UtcNow,
                QuestionsCount = 0,
                CorrectAnswersCount = 0
            };
            
            await _dbContext.Interviews.AddAsync(interview, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Interview создан с ID {InterviewId}", interview.InterviewId);
            
            // 3. Генерируем первый вопрос через GigaChat
            // Сначала создаем контекст без сохранения в менеджере сессий
            var context = new InterviewContext
            {
                InterviewId = interview.InterviewId,
                UserId = userid,
                JobTitle = request.JobTitle,
                JobLevel = request.JobLevel ?? "middle",
                JobDescription = request.JobDescription,
                StartedAt = DateTime.UtcNow
            };
            
            var firstQuestion = await _gigaChatService.GenerateQuestionAsync(
                request.JobDescription,
                request.JobTitle,
                request.JobLevel,
                context);
            
            var question = new InterviewQuestion
            {
                InterviewId = interview.InterviewId,
                QuestionText = firstQuestion,
                QuestionType = "technical",
                DifficultyLevel = "medium",
                TurnNumber = 1,
                AskedAt = DateTime.UtcNow
            };
            
            await _dbContext.InterviewQuestions.AddAsync(question, cancellationToken);
            
            interview.QuestionsCount = 1;
            _dbContext.Interviews.Update(interview);
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            //регаем сессию в менеджере
            context.AddMessage("assistant", firstQuestion);
            await _sessionManager.RegisterSessionAsync(context);
            
            await transaction.CommitAsync(cancellationToken);
            
            return new StartInterviewResponse(
                interview.InterviewId,
                firstQuestion,
                interview.StartedAt ?? DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Ошибка при запуске собеседования");
            throw;
        }
    }
}