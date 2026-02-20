using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Users.Command.RefreshToken;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class RefreshToken : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{RouteConsts.RefreshToken}",
            async (IMediator mediator, RefreshTokenCommand command) =>
            {
                Result<RefreshTokenResponse> result = await mediator.Send(new RefreshTokenCommand(
                command.Token,
                command.RefreshToken));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(RefreshToken))
            .WithTags(nameof(Users))
            .WithDescription("Refreshes an access token using a refresh token.")
            .WithSummary("Refreshes an access token using a refresh token.")
            .Produces<RefreshTokenResponse>(StatusCodes.Status200OK);
    }
}
