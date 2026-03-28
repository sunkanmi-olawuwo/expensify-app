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
using Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategory;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Categories.Query;

public sealed class GetExpenseCategory : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.CategoryById, async (IMediator mediator, ClaimsPrincipal claims, Guid categoryId) =>
            {
                Result<ExpenseCategoryResponse> result = await mediator.Send(new GetExpenseCategoryQuery(claims.GetUserId(), categoryId));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetExpenseCategory))
            .WithTags(nameof(Categories))
            .WithSummary("Gets an expense category by id.")
            .WithDescription("Returns an expense category owned by the current user.")
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<ExpenseCategoryResponse>(StatusCodes.Status200OK);
    }
}
