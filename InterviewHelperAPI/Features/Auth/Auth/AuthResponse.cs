namespace InterviewHelperAPI.Features.Auth.Auth;

public record AuthResponse(string? token, bool success, string message);