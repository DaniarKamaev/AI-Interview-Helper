using MediatR;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.GetAllInterview;

public record GetRequest() :  IRequest<GetResponse>;