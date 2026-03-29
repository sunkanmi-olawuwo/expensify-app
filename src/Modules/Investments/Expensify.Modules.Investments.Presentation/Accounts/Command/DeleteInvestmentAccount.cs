using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Investments.Application.Accounts.Command.DeleteInvestmentAccount;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Accounts.Command;

public sealed class DeleteInvestmentAccount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete(RouteConsts.InvestmentById, async (IMediator mediator, ClaimsPrincipal claims, Guid investmentId) =>
            {
                Result result = await mediator.Send(new DeleteInvestmentAccountCommand(claims.GetUserId(), investmentId));
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(DeleteInvestmentAccount))
            .WithTags("Investments")
            .WithSummary("Deletes an investment account.")
            .WithDescription("Soft-deletes an investment account and its contributions for the current user.")
            .RequireAuthorization(InvestmentPolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
