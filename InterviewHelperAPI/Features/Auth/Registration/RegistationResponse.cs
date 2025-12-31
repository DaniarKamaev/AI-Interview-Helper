namespace InterviewHelperAPI.Features.Auth.Registration;

public record RegistationResponse(string? token, bool success, string message);