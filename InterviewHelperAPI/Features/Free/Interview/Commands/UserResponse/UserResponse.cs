namespace InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse;

public record UserResponse(
    decimal Score,
    string? Feedback,
    List<string> DetectedSkills,
    List<string> ImprovementAreas,
    string? NextQuestionHint);