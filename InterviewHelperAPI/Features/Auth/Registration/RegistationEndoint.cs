using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InterviewHelperAPI.Features.Auth.Registration;

public static class RegistationEndoint
{
    public static void RegistationMap(this IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (
            [FromBody] RegistationRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var res = await mediator.Send(request, cancellationToken);
                return Results.Ok(res);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });
    }
}