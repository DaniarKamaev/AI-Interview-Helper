using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InterviewHelperAPI.Features.Auth.Auth;

public static class AuthEndpoint
{
    public static void AuthMap(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth", async (
            [FromBody] AuthRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var res = await mediator.Send(request, cancellationToken);
            return Results.Ok(res);
        });
    }
}