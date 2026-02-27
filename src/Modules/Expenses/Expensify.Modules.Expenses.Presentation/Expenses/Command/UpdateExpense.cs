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
using Expensify.Modules.Expenses.Application.Expenses.Command.UpdateExpense;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Expenses.Command;

public sealed class UpdateExpense : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.ExpenseById,
            async (IMediator mediator, ClaimsPrincipal claims, Guid expenseId, UpdateExpenseRequest request) =>
            {
                Result<ExpenseResponse> result = await mediator.Send(
                    new UpdateExpenseCommand(
                        claims.GetUserId(),
                        expenseId,
                        request.Amount,
                        request.Currency,
                        request.Date,
                        request.CategoryId,
                        request.Merchant,
                        request.Note,
                        request.PaymentMethod,
                        request.TagIds));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateExpense))
            .WithTags(nameof(Expenses))
            .WithSummary("Updates an expense.")
            .WithDescription("Updates an existing expense for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record UpdateExpenseRequest(
        decimal Amount,
        string Currency,
        DateOnly Date,
        Guid CategoryId,
        string Merchant,
        string Note,
        PaymentMethod PaymentMethod,
        IReadOnlyCollection<Guid> TagIds);
}
