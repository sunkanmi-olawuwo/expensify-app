using System.Security.Claims;
using Carter;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentAllocation;
using Expensify.Modules.Dashboard.Domain.Policies;
using Expensify.Modules.Dashboard.Presentation.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Expensify.Modules.Dashboard.Presentation.Dashboard.Analytics;

public sealed class GetDashboardInvestmentAllocation : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.InvestmentAllocation, async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result<DashboardInvestmentAllocationResponse> result =
                    await mediator.Send(new GetDashboardInvestmentAllocationQuery(claims.GetUserId()));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDashboardInvestmentAllocation))
            .WithTags("Dashboard")
            .WithSummary("Gets dashboard investment allocation.")
            .WithDescription("Returns portfolio allocation by investment category for the authenticated user.")
            .RequireAuthorization(DashboardPolicyConsts.ReadPolicy)
            .Produces<DashboardInvestmentAllocationResponse>(StatusCodes.Status200OK);
    }
}
