using System.Security.Claims;
using Carter;
using Carter.ModelBinding;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCashFlowTrend;
using Expensify.Modules.Dashboard.Domain.Policies;
using Expensify.Modules.Dashboard.Presentation.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Expensify.Modules.Dashboard.Presentation.Dashboard.Analytics;

public sealed class GetDashboardCashFlowTrend : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.CashFlowTrend, async (IMediator mediator, ClaimsPrincipal claims, [AsParameters] GetDashboardCashFlowTrendRequest request) =>
            {
                Result<DashboardCashFlowTrendResponse> result = await mediator.Send(
                    new GetDashboardCashFlowTrendQuery(claims.GetUserId(), request.Months));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDashboardCashFlowTrend))
            .WithTags("Dashboard")
            .WithSummary("Gets dashboard cash flow trend.")
            .WithDescription("Returns monthly income, expenses, net cash flow, and savings rate for the authenticated user.")
            .RequireAuthorization(DashboardPolicyConsts.ReadPolicy)
            .Produces<DashboardCashFlowTrendResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetDashboardCashFlowTrendRequest
    {
        [FromQuery(Name = "months")]
        public int Months { get; init; } = 6;
    }
}
