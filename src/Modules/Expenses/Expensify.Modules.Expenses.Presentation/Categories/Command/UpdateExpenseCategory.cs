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
using Expensify.Modules.Expenses.Application.Categories.Command.UpdateExpenseCategory;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Categories.Command;

public sealed class UpdateExpenseCategory : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.CategoryById, async (IMediator mediator, ClaimsPrincipal claims, Guid categoryId, CategoryBody body) =>
            {
                Result<ExpenseCategoryResponse> result = await mediator.Send(new UpdateExpenseCategoryCommand(claims.GetUserId(), categoryId, body.Name));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateExpenseCategory))
            .WithTags(nameof(Categories))
            .WithSummary("Updates an expense category.")
            .WithDescription("Updates an existing expense category for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
