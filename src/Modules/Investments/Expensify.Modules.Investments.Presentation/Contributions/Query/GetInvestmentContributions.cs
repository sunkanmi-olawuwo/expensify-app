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
using Expensify.Modules.Investments.Application.Contributions.Query.GetInvestmentContributions;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Contributions.Query;

public sealed class GetInvestmentContributions : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Contributions, async (IMediator mediator, HttpContext context, Guid investmentId, [AsParameters] GetInvestmentContributionsRequest request) =>
            {
                Result<InvestmentContributionsPageResponse> result = await mediator.Send(
                    new GetInvestmentContributionsQuery(
                        context.User.GetUserId(),
                        investmentId,
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
            .WithName(nameof(GetInvestmentContributions))
            .WithTags("Investments")
            .WithSummary("Gets investment contributions.")
            .WithDescription("Returns paged contributions for an investment account owned by the current user.")
            .RequireAuthorization(InvestmentPolicyConsts.ReadPolicy)
            .Produces<InvestmentContributionsPageResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetInvestmentContributionsRequest
    {
        [FromQuery(Name = "page")]
        public int Page { get; init; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; init; } = 20;
    }
}
