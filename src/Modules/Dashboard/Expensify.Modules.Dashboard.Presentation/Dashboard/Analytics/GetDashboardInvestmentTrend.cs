using System.Security.Claims;
using Carter;
using Carter.ModelBinding;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentTrend;
using Expensify.Modules.Dashboard.Domain.Policies;
using Expensify.Modules.Dashboard.Presentation.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Expensify.Modules.Dashboard.Presentation.Dashboard.Analytics;

public sealed class GetDashboardInvestmentTrend : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.InvestmentTrend, async (IMediator mediator, ClaimsPrincipal claims, [AsParameters] GetDashboardInvestmentTrendRequest request) =>
            {
                Result<DashboardInvestmentTrendResponse> result = await mediator.Send(
                    new GetDashboardInvestmentTrendQuery(claims.GetUserId(), request.Months));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDashboardInvestmentTrend))
            .WithTags("Dashboard")
            .WithSummary("Gets dashboard investment trend.")
            .WithDescription("Returns contribution totals by month for the authenticated user.")
            .RequireAuthorization(DashboardPolicyConsts.ReadPolicy)
            .Produces<DashboardInvestmentTrendResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetDashboardInvestmentTrendRequest
    {
        [FromQuery(Name = "months")]
        public int Months { get; init; } = 6;
    }
}
