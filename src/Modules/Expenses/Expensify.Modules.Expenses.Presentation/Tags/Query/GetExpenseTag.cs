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
using Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTag;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Tags.Query;

public sealed class GetExpenseTag : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.TagById, async (IMediator mediator, ClaimsPrincipal claims, Guid tagId) =>
            {
                Result<ExpenseTagResponse> result = await mediator.Send(new GetExpenseTagQuery(claims.GetUserId(), tagId));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetExpenseTag))
            .WithTags(nameof(Tags))
            .WithSummary("Gets an expense tag by id.")
            .WithDescription("Returns an expense tag owned by the current user.")
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<ExpenseTagResponse>(StatusCodes.Status200OK);
    }
}
