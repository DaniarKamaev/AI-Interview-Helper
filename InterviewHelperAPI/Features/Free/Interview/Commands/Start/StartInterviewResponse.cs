namespace InterviewHelperAPI.Features.Free.Interview.Commands;

public record StartInterviewResponse(
    int InterviewId,
    string FirstQuestion,
    int QuestionId,
    DateTime StartedAt);