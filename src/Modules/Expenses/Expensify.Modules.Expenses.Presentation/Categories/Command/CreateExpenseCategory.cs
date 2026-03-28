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
using Expensify.Modules.Expenses.Application.Categories.Command.CreateExpenseCategory;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Categories.Command;

public sealed class CreateExpenseCategory : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Categories, async (IMediator mediator, ClaimsPrincipal claims, CategoryBody body) =>
            {
                Result<ExpenseCategoryResponse> result = await mediator.Send(new CreateExpenseCategoryCommand(claims.GetUserId(), body.Name));
                return result.Match(
                    response => Results.Created($"{RouteConsts.Categories}/{response.Id}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(CreateExpenseCategory))
            .WithTags(nameof(Categories))
            .WithSummary("Creates a new expense category.")
            .WithDescription("Creates a new expense category for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
