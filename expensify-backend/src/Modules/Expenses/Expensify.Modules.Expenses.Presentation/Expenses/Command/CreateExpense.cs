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
using Expensify.Modules.Expenses.Application.Expenses.Command.CreateExpense;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Expenses.Command;

public sealed class CreateExpense : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Expenses, async (IMediator mediator, ClaimsPrincipal claims, CreateExpenseRequest request) =>
            {
                Result<ExpenseResponse> result = await mediator.Send(
                    new CreateExpenseCommand(
                        claims.GetUserId(),
                        request.Amount,
                        request.Currency,
                        request.Date,
                        request.CategoryId,
                        request.Merchant,
                        request.Note,
                        request.PaymentMethod,
                        request.TagIds));

                return result.Match(
                    response => Results.Created($"{RouteConsts.Expenses}/{response.Id}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(CreateExpense))
            .WithTags(nameof(Expenses))
            .WithSummary("Creates a new expense.")
            .WithDescription("Creates a new expense record for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseResponse>(StatusCodes.Status201Created);
    }

    public sealed record CreateExpenseRequest(
        decimal Amount,
        string Currency,
        DateOnly Date,
        Guid CategoryId,
        string Merchant,
        string Note,
        PaymentMethod PaymentMethod,
        IReadOnlyCollection<Guid> TagIds);
}
