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
            var interviewId = AuthHelper.GetCurrentInterviewId(_httpContextAccessor);
            if (interviewId == null || interviewId == 0)
            {
                throw new Exception("Такого интерьвю нету");
            }

            var interview = await _db.Interviews
                .Include(i => i.InterviewQuestions)
                .FirstOrDefaultAsync(x => x.InterviewId == interviewId, cancellationToken);

            if (interview == null)
            {
                throw new Exception($"Интервью с id {interviewId} не найдено");
            }


            var lastQuestion = interview.InterviewQuestions
                .Where(q => q.AskedAt != null && !_db.UserResponses.Any(ur => ur.QuestionId == q.QuestionId))
                .OrderByDescending(q => q.AskedAt)
                .FirstOrDefault();

            if (lastQuestion == null)
            {
                throw new Exception("Нет акивного интерьвю");
            }

            var context = await _sessionManager.GetSessionAsync((int)interviewId);
            if (context == null)
            {
                context = await _repository.RestoreSessionFromDatabase((int)interviewId);
                await _sessionManager.RegisterSessionAsync(context);
            }

            var evaluation = await _gigaChatService
                .EvaluateAnswerAsync(
                    question: lastQuestion.QuestionText,
                    userAnswer: request.UserAnswer,
                    context: context);
            var userResponse = new InterviewHelperAPI.UserResponse
            {
                QuestionId = lastQuestion.QuestionId,
                InterviewId = (int)interviewId,
                UserAnswer = request.UserAnswer,
                AnsweredAt = DateTime.UtcNow,
                AiComment = evaluation.Feedback,
                DetectedSkills = JsonSerializer.Serialize(evaluation.DetectedSkills),
                AiAnalysis = JsonSerializer.Serialize(new
                {
                    Score = evaluation.Score,
                    DetailedScores = evaluation.DetailedScores,
                    ImprovementAreas = evaluation.ImprovementAreas
                })
            };

            await _db.UserResponses.AddAsync(userResponse, cancellationToken);

            context.AddMessage("user", request.UserAnswer);
            await _sessionManager.UpdateSessionAsync(context);

            string nextQuestion = string.Empty;
            if (interview.QuestionsCount < 10)
            {
                nextQuestion = await _gigaChatService.GenerateQuestionAsync(
                    jobDescription: context.JobDescription,
                    jobTitle: context.JobTitle,
                    jobLevel: context.JobLevel,
                    context: context);

                var nextQuestionEntity = new InterviewQuestion
                {
                    InterviewId = (int)interviewId,
                    QuestionText = nextQuestion,
                    QuestionType = "technical",
                    DifficultyLevel = _repository.GetDifficultyBasedOnScore(evaluation.Score),
                    TurnNumber = interview.InterviewQuestions.Count + 1,
                    AskedAt = DateTime.UtcNow
                };

                await _db.InterviewQuestions.AddAsync(nextQuestionEntity, cancellationToken);
                context.AddMessage("assistant", nextQuestion);
                await _sessionManager.UpdateSessionAsync(context);

                interview.QuestionsCount = (interview.QuestionsCount ?? 0) + 1;
            }
            else
            {
                await _repository.CompleteInterview(interview, context, evaluation.Score);
                nextQuestion = "[INTERVIEW_COMPLETED]";
            }

            if (evaluation.Score >= 7)
            {
                interview.CorrectAnswersCount = (interview.CorrectAnswersCount ?? 0) + 1;
            }

            await _repository.SaveSkillEvaluations((int)interviewId, evaluation, context);

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
                    : evaluation.NextQuestionHint
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new Exception($"Error processing response: {ex.Message}");
        }
    }
}