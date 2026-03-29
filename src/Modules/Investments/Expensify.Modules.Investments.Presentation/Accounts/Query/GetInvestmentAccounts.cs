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
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Accounts.Query.GetInvestmentAccounts;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Accounts.Query;

public sealed class GetInvestmentAccounts : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Investments, async (IMediator mediator, HttpContext context, [AsParameters] GetInvestmentAccountsRequest request) =>
            {
                Result<InvestmentAccountsPageResponse> result = await mediator.Send(
                    new GetInvestmentAccountsQuery(
                        context.User.GetUserId(),
                        request.CategoryId,
                        request.Page,
                        request.PageSize));

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
            .WithName(nameof(GetInvestmentAccounts))
            .WithTags("Investments")
            .WithSummary("Gets investment accounts.")
            .WithDescription("Returns paged investment accounts for the current user with an optional category filter.")
            .RequireAuthorization(InvestmentPolicyConsts.ReadPolicy)
            .Produces<InvestmentAccountsPageResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetInvestmentAccountsRequest
    {
        [FromQuery(Name = "categoryId")]
        public Guid? CategoryId { get; init; }

        [FromQuery(Name = "page")]
        public int Page { get; init; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; init; } = 20;
    }
}
