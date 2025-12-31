using MediatR;
namespace InterviewHelperAPI.Features.Auth.Registration;

public record RegistationRequest(
    string Username,
    string Email,
    string Password,
    string? SubscriptionTier) : IRequest<RegistationResponse>;