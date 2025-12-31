using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InterviewHelperAPI.Features.Free.Interview.Commands;

public static class StartInterviewEndpoint
{
    public static void StartInterviewMap(this IEndpointRouteBuilder app)
    {
        app.MapPost("/interview/start", async (
            [FromBody] StartInterviewCommand requset,
            IMediator mediator,
            CancellationToken token) =>
        {
            try
            {
                var res = await mediator.Send(requset, token);
                return Results.Ok(res);
            }
            catch(Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        })
            .RequireAuthorization();
    }
}