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
using Expensify.Modules.Income.Application.Incomes.Command.DeleteIncome;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Incomes.Command;

public sealed class DeleteIncome : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete(RouteConsts.IncomeById, async (IMediator mediator, ClaimsPrincipal claims, Guid incomeId) =>
            {
                Result result = await mediator.Send(new DeleteIncomeCommand(claims.GetUserId(), incomeId));
                return result.Match(() => Results.NoContent(), ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(DeleteIncome))
            .WithTags("Income")
            .WithSummary("Deletes an income.")
            .WithDescription("Deletes an income record for the current user.")
            .RequireAuthorization(IncomePolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
