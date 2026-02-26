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
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Query.GetExpense;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Expenses.Query;

public sealed class GetExpense : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.ExpenseById, async (IMediator mediator, ClaimsPrincipal claims, Guid expenseId) =>
            {
                Result<ExpenseResponse> result = await mediator.Send(new GetExpenseQuery(claims.GetUserId(), expenseId));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetExpense))
            .WithTags(nameof(Expenses))
            .WithSummary("Gets an expense by id.")
            .WithDescription("Returns an expense owned by the current user.")
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<ExpenseResponse>(StatusCodes.Status200OK);
    }
}
