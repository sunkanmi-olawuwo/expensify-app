using System.Security.Claims;
using Carter;
using Carter.ModelBinding;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardIncomeBreakdown;
using Expensify.Modules.Dashboard.Domain.Policies;
using Expensify.Modules.Dashboard.Presentation.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Expensify.Modules.Dashboard.Presentation.Dashboard.Analytics;

public sealed class GetDashboardIncomeBreakdown : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.IncomeBreakdown, async (IMediator mediator, ClaimsPrincipal claims, [AsParameters] GetDashboardIncomeBreakdownRequest request) =>
            {
                Result<DashboardIncomeBreakdownResponse> result = await mediator.Send(
                    new GetDashboardIncomeBreakdownQuery(claims.GetUserId(), request.Months));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDashboardIncomeBreakdown))
            .WithTags("Dashboard")
            .WithSummary("Gets dashboard income breakdown.")
            .WithDescription("Returns income totals grouped by type for the authenticated user.")
            .RequireAuthorization(DashboardPolicyConsts.ReadPolicy)
            .Produces<DashboardIncomeBreakdownResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetDashboardIncomeBreakdownRequest
    {
        [FromQuery(Name = "months")]
        public int Months { get; init; } = 3;
    }
}
