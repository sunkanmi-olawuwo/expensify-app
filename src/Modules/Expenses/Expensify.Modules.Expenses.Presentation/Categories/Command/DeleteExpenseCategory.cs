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
using Expensify.Modules.Expenses.Application.Categories.Command.DeleteExpenseCategory;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Categories.Command;

public sealed class DeleteExpenseCategory : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete(RouteConsts.CategoryById, async (IMediator mediator, ClaimsPrincipal claims, Guid categoryId) =>
            {
                Result result = await mediator.Send(new DeleteExpenseCategoryCommand(claims.GetUserId(), categoryId));
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(DeleteExpenseCategory))
            .WithTags(nameof(Categories))
            .WithSummary("Deletes an expense category.")
            .WithDescription("Deletes an existing expense category for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
