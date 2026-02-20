using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Users.Command.UpdateUser;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class UpdateUserProfile : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut($"{RouteConsts.UpdateUser}",
            async (IMediator mediator, ClaimsPrincipal claims, [FromRoute] Guid id, [FromBody] UpdateUserData data) =>
            {
                Result result = await mediator.Send(new UpdateUserCommand(
                claims.GetUserId(),
                data.FirstName,
                data.LastName));

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateUserProfile))
            .WithTags(nameof(Users))
            .WithDescription("Updates a user's profile using the userId and other details")
            .WithSummary("Updates a user's information.")
            .RequireAuthorization(UserPolicyConsts.UpdatePolicy)
            .Produces(StatusCodes.Status204NoContent);
    }

    public record UpdateUserData(string FirstName, string LastName);
}
