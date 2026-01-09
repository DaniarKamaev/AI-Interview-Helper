namespace InterviewHelperAPI.Features.Free.Interview.Commands.GetAllInterview;

public record GetResponse(
    int Count,
    List<InterviewHelperAPI.Interview> Interviews);