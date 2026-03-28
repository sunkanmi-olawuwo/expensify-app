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
using Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTags;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Tags.Query;

public sealed class GetExpenseTags : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Tags, async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result<IReadOnlyCollection<ExpenseTagResponse>> result = await mediator.Send(new GetExpenseTagsQuery(claims.GetUserId()));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetExpenseTags))
            .WithTags(nameof(Tags))
            .WithSummary("Gets all expense tags.")
            .WithDescription("Returns all expense tags owned by the current user.")
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<IReadOnlyCollection<ExpenseTagResponse>>(StatusCodes.Status200OK);
    }
}
