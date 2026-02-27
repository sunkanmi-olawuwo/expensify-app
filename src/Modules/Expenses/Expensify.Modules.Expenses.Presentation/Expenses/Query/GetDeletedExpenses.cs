using System.Globalization;
using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Query.GetDeletedExpenses;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Expenses.Query;

public sealed class GetDeletedExpenses : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.RecycleBinExpenses, async (IMediator mediator, ClaimsPrincipal claims, HttpContext context, [AsParameters] GetDeletedExpensesRequest request) =>
            {
                Result<DeletedExpensesPageResponse> result = await mediator.Send(
                    new GetDeletedExpensesQuery(
                        claims.GetUserId(),
                        request.Page,
                        request.PageSize));

                return result.Match(response =>
                {
                    context.Response.Headers.Append("X-Pagination-CurrentPage", response.CurrentPage.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-PageSize", response.PageSize.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-TotalCount", response.TotalCount.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-TotalPages", response.TotalPages.ToString(CultureInfo.InvariantCulture));
                    return Results.Ok(response);
                }, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDeletedExpenses))
            .WithTags(nameof(Expenses))
            .WithSummary("Gets deleted expenses from recycle bin.")
            .WithDescription("Returns paged deleted expenses for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<DeletedExpensesPageResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetDeletedExpensesRequest
    {
        [FromQuery(Name = "page")]
        public int Page { get; init; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; init; } = 20;
    }
}
