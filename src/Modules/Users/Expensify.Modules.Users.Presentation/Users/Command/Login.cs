using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Users.Command.Login;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class Login : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{RouteConsts.Login}",
            async (IMediator mediator, LoginCommand command) =>
            {
                Result<LoginUserResponse> result = await mediator.Send(command);

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(Login))
            .WithTags(nameof(Users))
            .WithDescription("Logs in a user with email and password.")
            .WithSummary("Logs in a user.")
            .Produces<LoginUserResponse>(StatusCodes.Status200OK);

    }
}
