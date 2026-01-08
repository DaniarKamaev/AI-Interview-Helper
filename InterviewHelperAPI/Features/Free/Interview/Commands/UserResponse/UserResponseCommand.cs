using MediatR;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse;

public record UserResponseCommand(
    string UserAnswer,
    int QuestionId) : IRequest<UserResponse>;