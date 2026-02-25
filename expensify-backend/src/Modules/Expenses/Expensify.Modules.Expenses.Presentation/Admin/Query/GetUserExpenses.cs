using System.Globalization;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Admin.Query.GetUserExpenses;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Admin.Query;

public sealed class GetUserExpenses : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.AdminExpenses, async (IMediator mediator, HttpContext context, Guid userId, [AsParameters] GetUserExpensesRequest request) =>
            {
                Result<ExpensesPageResponse> result = await mediator.Send(
                    new GetUserExpensesQuery(
                        userId,
                        request.Period,
                        request.CategoryId,
                        request.Merchant,
                        request.TagIds,
                        request.MinAmount,
                        request.MaxAmount,
                        request.PaymentMethod,
                        request.SortBy,
                        request.SortOrder,
                        request.Page,
                        request.PageSize));

                return result.Match(response =>
                {
                    context.Response.Headers.Append("X-Pagination-CurrentPage", response.CurentPage.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-PageSize", response.PageSize.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-TotalCount", response.TotalCount.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-TotalPages", response.TotalPages.ToString(CultureInfo.InvariantCulture));
                    return Results.Ok(response);
                }, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetUserExpenses))
            .WithTags(nameof(Expenses))
            .WithSummary("Gets paginated expenses for a specific user (admin).")
            .WithDescription("Admin-only endpoint to read expenses for a given user and period.")
            .RequireAuthorization(ExpensePolicyConsts.AdminReadPolicy)
            .Produces<ExpensesPageResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetUserExpensesRequest
    {
        [FromQuery(Name = "period")]
        public string Period { get; init; } = string.Empty;

        [FromQuery(Name = "categoryId")]
        public Guid? CategoryId { get; init; }

        [FromQuery(Name = "merchant")]
        public string Merchant { get; init; } = string.Empty;

        [FromQuery(Name = "tagIds")]
        public Guid[]? TagIds { get; init; }

        [FromQuery(Name = "minAmount")]
        public decimal? MinAmount { get; init; }

        [FromQuery(Name = "maxAmount")]
        public decimal? MaxAmount { get; init; }

        [FromQuery(Name = "paymentMethod")]
        public string PaymentMethod { get; init; } = string.Empty;

        [FromQuery(Name = "sortBy")]
        public string SortBy { get; init; } = "date";

        [FromQuery(Name = "sortOrder")]
        public string SortOrder { get; init; } = "desc";

        [FromQuery(Name = "page")]
        public int Page { get; init; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; init; } = 20;
    }
}
