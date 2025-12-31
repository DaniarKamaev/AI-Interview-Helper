using MediatR;
namespace InterviewHelperAPI.Features.Auth.Auth;

public record AuthRequest(
    string Email,
    string Password): IRequest<AuthResponse>;