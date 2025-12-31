using InterviewHelperAPI.Service.GigaChat;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse.Db;

public interface IRepository
{
    Task<InterviewContext> RestoreSessionFromDatabase(int interviewId);
    Task CompleteInterview(InterviewHelperAPI.Interview interview, InterviewContext context, decimal finalScore);
    Task UpdateUserStatistics(int userId, decimal score);
    string GetDifficultyBasedOnScore(decimal score);
    Task SaveSkillEvaluations(int interviewId, AnswerEvaluation evaluation, InterviewContext context);
    string GetSkillCategory(string skillName);
}