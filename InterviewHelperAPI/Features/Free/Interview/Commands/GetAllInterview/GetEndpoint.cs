using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InterviewHelperAPI.Features.Free.Interview.Commands.GetAllInterview;

public static class GetEndpoint
{
    public static void GetInterviewMap(this IEndpointRouteBuilder app)
    {
        app.MapGet("/get/interview", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var request = new GetRequest();
                var response = await mediator.Send(request, cancellationToken);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
                
        })
        .RequireAuthorization();
    }
}