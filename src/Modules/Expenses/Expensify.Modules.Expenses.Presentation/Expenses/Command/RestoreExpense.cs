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
using Expensify.Modules.Expenses.Application.Expenses.Command.RestoreExpense;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Expenses.Command;

public sealed class RestoreExpense : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.RestoreExpenseById, async (IMediator mediator, ClaimsPrincipal claims, Guid expenseId) =>
            {
                Result result = await mediator.Send(new RestoreExpenseCommand(claims.GetUserId(), expenseId));
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(RestoreExpense))
            .WithTags(nameof(Expenses))
            .WithSummary("Restores a deleted expense.")
            .WithDescription("Restores a deleted expense owned by the current user.")
            .RequireAuthorization(ExpensePolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent);
    }
}