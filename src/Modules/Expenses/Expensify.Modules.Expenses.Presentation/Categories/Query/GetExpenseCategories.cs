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
using Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategories;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Categories.Query;

public sealed class GetExpenseCategories : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Categories, async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result<IReadOnlyCollection<ExpenseCategoryResponse>> result = await mediator.Send(new GetExpenseCategoriesQuery(claims.GetUserId()));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetExpenseCategories))
            .WithTags(nameof(Categories))
            .WithSummary("Gets all expense categories.")
            .WithDescription("Returns all expense categories owned by the current user.")
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<IReadOnlyCollection<ExpenseCategoryResponse>>(StatusCodes.Status200OK);
    }
}
