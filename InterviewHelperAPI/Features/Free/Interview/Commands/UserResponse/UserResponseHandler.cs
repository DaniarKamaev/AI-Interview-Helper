using System.Text.Json;
using InterviewHelperAPI.Features.Auth;
using InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse.Db;
using InterviewHelperAPI.Service.GigaChat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse;

public class UserResponseHandler : IRequestHandler<UserResponseCommand, UserResponse>
{
    private readonly IGigaChatService _gigaChatService;
    private readonly HelperDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IInterviewSessionManager  _sessionManager;
    private readonly IRepository _repository;

    public UserResponseHandler(
        IGigaChatService gigaChatService,
        HelperDbContext db,
        IHttpContextAccessor httpContextAccessor,
        IInterviewSessionManager  sessionManager,
        IRepository repository)
    {
        _gigaChatService = gigaChatService;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _sessionManager = sessionManager;
        _repository = repository;
    }


    public async Task<UserResponse> Handle(UserResponseCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            int userId = AuthHelper.GetCurrentUserId(_httpContextAccessor);
            if (userId == 0)
            {
                throw new Exception("Такого интерьвю нету");
            }

            var interviewQuery = await _db.InterviewQuestions
                .Where(q => q.QuestionId == request.QuestionId)
                .Select(q => new
                {
                    q.InterviewId,
                    q.Interview.UserId,
                    q.Interview
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (interviewQuery == null)
            {
                throw new Exception($"Интервью с id {request.QuestionId} не найдено");
            }
            
            var interviewId = interviewQuery.InterviewId;
        
            
            
            var interview = await _db.Interviews
                .Include(i => i.InterviewQuestions)
                .FirstOrDefaultAsync(x => x.InterviewId == interviewId, cancellationToken);

            if (interview == null)
            {
                throw new Exception($"Интервью с id {interviewId} не найдено");
            }

            
            if (request.QuestionId == 0)
            {
                throw new Exception("QuestionId не указан в запросе");
            }

            var currentQuestion = await _db.InterviewQuestions
                .Where(q => q.QuestionId == request.QuestionId && q.InterviewId == interviewId)
                .Select(q => new
                {
                    q.QuestionId,
                    q.TurnNumber,
                    q.QuestionText,
                    HasResponse = _db.UserResponses.Any(ur => ur.QuestionId == q.QuestionId)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (currentQuestion == null)
            {
                throw new Exception($"Вопрос с ID {request.QuestionId} не найден для интервью {interviewId}");
            }

            if (currentQuestion.HasResponse)
            {
                throw new Exception($"На вопрос с ID {request.QuestionId} уже получен ответ");
            }

            var context = await _sessionManager.GetSessionAsync(interviewId);
            if (context == null)
            {
                context = await _repository.RestoreSessionFromDatabase(interviewId);
                await _sessionManager.RegisterSessionAsync(context);
            }

            var evaluation = await _gigaChatService
                .EvaluateAnswerAsync(
                    question: currentQuestion.QuestionText,
                    userAnswer: request.UserAnswer,
                    context: context);
            
            string nextQuestion = string.Empty;
            int? nextQuestionId = null;
    
            var currentTurnNumber = interview.InterviewQuestions
                .OrderByDescending(q => q.TurnNumber)
                .Select(q => q.TurnNumber)
                .FirstOrDefault();
            if ((interview.QuestionsCount ?? 0) < 10) 
            {
                // ИСПРАВЛЕНИЕ: используем подсказку из оценки как основу для следующего вопроса
                // либо генерируем вопрос на основе этой подсказки
                nextQuestion = await _gigaChatService.GenerateQuestionFromHintAsync(
                    jobDescription: context.JobDescription,
                    jobTitle: context.JobTitle,
                    jobLevel: context.JobLevel,
                    hint: evaluation.NextQuestionHint,
                    context: context);
        
                var nextQuestionEntity = new InterviewQuestion
                {
                    InterviewId = interviewId,
                    QuestionText = nextQuestion,
                    QuestionType = "technical",
                    DifficultyLevel = _repository.GetDifficultyBasedOnScore(evaluation.Score),
                    TurnNumber = currentTurnNumber + 1,
                    AskedAt = DateTime.UtcNow
                };
        
                await _db.InterviewQuestions.AddAsync(nextQuestionEntity, cancellationToken);
                interview.InterviewQuestions.Add(nextQuestionEntity);
                nextQuestionId = nextQuestionEntity.QuestionId;
        
                context.AddMessage("assistant", nextQuestion);
                await _sessionManager.UpdateSessionAsync(context);
                interview.QuestionsCount = (interview.QuestionsCount ?? 0) + 1;
            }
            else
            {
                interview.Status = "completed";
                interview.CompletedAt = DateTime.UtcNow;
                await _repository.CompleteInterview(interview, context, evaluation.Score);
                nextQuestion = "[INTERVIEW_COMPLETED]";
            }


            context.AddMessage("user", request.UserAnswer);
            await _sessionManager.UpdateSessionAsync(context);
            
            

            if (evaluation.Score >= 7)
            {
                interview.CorrectAnswersCount = (interview.CorrectAnswersCount ?? 0) + 1;
            }

            await _repository.SaveSkillEvaluations(interviewId, evaluation, context);

            await _repository.UpdateUserStatistics(interview.UserId, evaluation.Score);

            _db.Interviews.Update(interview);
            await _db.SaveChangesAsync(cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);

            return new UserResponse(
                Score: evaluation.Score,
                Feedback: evaluation.Feedback,
                DetectedSkills: evaluation.DetectedSkills,
                ImprovementAreas: evaluation.ImprovementAreas,
                NextQuestionHint: nextQuestion == "[INTERVIEW_COMPLETED]"
                    ? "Собеседование завершено. Вы можете просмотреть итоги."
                    : nextQuestion,
                NextQuestionId: nextQuestionId
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new Exception($"Error processing response: {ex.Message}");
        }
    }
}