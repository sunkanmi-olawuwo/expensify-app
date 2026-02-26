using Carter;
using Carter.ModelBinding;
using MediatR;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Incomes.Query.GetIncomes;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Incomes.Query;

public sealed class GetIncomes : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Income, async (IMediator mediator, HttpContext context, [AsParameters] GetIncomesRequest request) =>
            {
                Result<IncomePageResponse> result = await mediator.Send(
                    new GetIncomesQuery(
                        context.User.GetUserId(),
                        request.Period,
                        request.Source,
                        request.Type,
                        request.MinAmount,
                        request.MaxAmount,
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
            .WithName(nameof(GetIncomes))
            .WithTags("Income")
            .WithSummary("Gets incomes for a period.")
            .WithDescription("Returns paged income records for the selected period with optional filters.")
            .RequireAuthorization(IncomePolicyConsts.ReadPolicy)
            .Produces<IncomePageResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetIncomesRequest
    {
        [FromQuery(Name = "period")]
        public string Period { get; init; } = string.Empty;

        [FromQuery(Name = "source")]
        public string Source { get; init; } = string.Empty;

        [FromQuery(Name = "type")]
        public IncomeType? Type { get; init; }

        [FromQuery(Name = "minAmount")]
        public decimal? MinAmount { get; init; }

        [FromQuery(Name = "maxAmount")]
        public decimal? MaxAmount { get; init; }

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

