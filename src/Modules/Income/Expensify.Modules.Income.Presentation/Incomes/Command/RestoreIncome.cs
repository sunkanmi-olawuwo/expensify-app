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
using Expensify.Modules.Income.Application.Incomes.Command.RestoreIncome;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Incomes.Command;

public sealed class RestoreIncome : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.RestoreIncomeById, async (IMediator mediator, ClaimsPrincipal claims, Guid incomeId) =>
            {
                Result result = await mediator.Send(new RestoreIncomeCommand(claims.GetUserId(), incomeId));
                return result.Match(() => Results.NoContent(), ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(RestoreIncome))
            .WithTags("Income")
            .WithSummary("Restores a deleted income.")
            .WithDescription("Restores a deleted income record for the current user.")
            .RequireAuthorization(IncomePolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
