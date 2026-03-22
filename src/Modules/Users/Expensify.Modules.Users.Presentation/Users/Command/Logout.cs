using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Infrastructure.RateLimiting;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Users.Command.Logout;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class Logout : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{RouteConsts.Logout}",
            async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result result = await mediator.Send(new LogoutCommand(claims.GetUserId()));

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(Logout))
            .WithTags(nameof(Users))
            .WithDescription("Logs out the current user from every active session.")
            .WithSummary("Logs out the current user.")
            .RequireAuthorization(UserPolicyConsts.UpdatePolicy)
            .RequireRateLimiting(RateLimitingPolicyNames.AuthPolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
