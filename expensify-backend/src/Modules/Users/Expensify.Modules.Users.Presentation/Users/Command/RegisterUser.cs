using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Users.Command.RegisterUser;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class RegisterUser : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{RouteConsts.Register}",
            async (IMediator mediator, RegisterUserCommand command) =>
            {
                Result<RegisterUserResponse> result = await mediator.Send(new RegisterUserCommand(
                command.Email,
                command.Password,
                command.FirstName,
                command.LastName,
                command.Role));

                return result.Match(userId => Results.Created($"{RouteConsts.Register}/{userId}", userId), ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(RegisterUser))
            .WithTags(nameof(Users))
            .WithDescription("Registers a new user with email, password, and other required information.")
            .WithSummary("Registers a new user.")
            .Produces<RegisterUserResponse>(StatusCodes.Status201Created);
    }
}
