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
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Users.Command.ChangePassword;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class ChangePassword : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{RouteConsts.ChangePassword}",
            async (IMediator mediator, ClaimsPrincipal claims, ChangePasswordRequest request) =>
            {
                Result result = await mediator.Send(new ChangePasswordCommand(
                    claims.GetUserId(),
                    request.CurrentPassword,
                    request.NewPassword));

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(ChangePassword))
            .WithTags(nameof(Users))
            .WithDescription("Changes the current user's password and invalidates all active sessions.")
            .WithSummary("Changes the current user's password.")
            .RequireAuthorization(UserPolicyConsts.UpdatePolicy)
            .RequireRateLimiting(RateLimitingPolicyNames.AuthPolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
