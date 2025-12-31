using MediatR;

namespace InterviewHelperAPI.Features.Free.Interview.Commands;

public record StartInterviewCommand(
    int UserId,
    string JobTitle,
    string JobDescription,
    string JobLevel = "middle") : IRequest<StartInterviewResponse>;