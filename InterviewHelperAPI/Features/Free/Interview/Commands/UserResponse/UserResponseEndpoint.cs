using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.UserResponse;

public static class UserResponseEndpoint
{
    public static void UserResponseMap(this IEndpointRouteBuilder app)
    {
        app.MapPost("/interview/{interviewId:int}/response", async (
                int interviewId,
                [FromBody] UserResponseCommand request,
                IMediator mediator,
                CancellationToken token) =>
            {
                try
                {
                    var response = await mediator.Send(request, token);
                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .RequireAuthorization();
    }
}