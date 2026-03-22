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
using Expensify.Modules.Users.Application.Users.Command.ForgotPassword;

namespace Expensify.Modules.Users.Presentation.Users.Command;

public class ForgotPassword : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost($"{RouteConsts.ForgotPassword}",
            async (IMediator mediator, ForgotPasswordRequest request) =>
            {
                Result result = await mediator.Send(new ForgotPasswordCommand(request.Email));

                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .AllowAnonymous()
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(ForgotPassword))
            .WithTags(nameof(Users))
            .WithDescription("Starts a password reset workflow for a user email.")
            .WithSummary("Starts a password reset workflow.")
            .RequireRateLimiting(RateLimitingPolicyNames.AuthPolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
