using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.RateLimiting;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Users.Command.ResetPassword;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class ResetPassword : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{RouteConsts.ResetPassword}",
            async (IMediator mediator, ResetPasswordRequest request) =>
            {
                Result result = await mediator.Send(new ResetPasswordCommand(
                    request.Email,
                    request.Token,
                    request.NewPassword));

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .AllowAnonymous()
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(ResetPassword))
            .WithTags(nameof(Users))
            .WithDescription("Completes a password reset using an email and reset token.")
            .WithSummary("Resets a user's password.")
            .RequireRateLimiting(RateLimitingPolicyNames.AuthPolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
