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
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Admin.Query.GetInvestments;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Admin.Query;

public sealed class GetInvestments : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.AdminInvestments, async (IMediator mediator, HttpContext context, [AsParameters] GetInvestmentsRequest request) =>
            {
                Result<InvestmentAccountsPageResponse> result = await mediator.Send(new GetInvestmentsQuery(request.Page, request.PageSize));

                return result.Match(response =>
                {
                    context.Response.Headers.Append("X-Pagination-CurrentPage", response.Page.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-PageSize", response.PageSize.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-TotalCount", response.TotalCount.ToString(CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-Pagination-TotalPages", response.TotalPages.ToString(CultureInfo.InvariantCulture));

                    return Results.Ok(response);
                }, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetInvestments))
            .WithTags("Investments")
            .WithSummary("Gets investment accounts for admins.")
            .WithDescription("Returns paged investment accounts across users for administrators.")
            .RequireAuthorization(InvestmentPolicyConsts.AdminReadPolicy)
            .Produces<InvestmentAccountsPageResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetInvestmentsRequest
    {
        [FromQuery(Name = "page")]
        public int Page { get; init; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; init; } = 20;
    }
}
