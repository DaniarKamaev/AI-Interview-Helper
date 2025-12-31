using MediatR;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse;

public record UserResponseCommand(
    string UserAnswer) : IRequest<UserResponse>;