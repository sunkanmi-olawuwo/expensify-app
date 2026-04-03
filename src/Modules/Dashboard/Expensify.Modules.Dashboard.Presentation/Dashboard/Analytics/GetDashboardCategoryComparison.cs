using System.Security.Claims;
using Carter;
using Carter.ModelBinding;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCategoryComparison;
using Expensify.Modules.Dashboard.Domain.Policies;
using Expensify.Modules.Dashboard.Presentation.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Expensify.Modules.Dashboard.Presentation.Dashboard.Analytics;

public sealed class GetDashboardCategoryComparison : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.CategoryComparison, async (IMediator mediator, ClaimsPrincipal claims, [AsParameters] GetDashboardCategoryComparisonRequest request) =>
            {
                Result<DashboardCategoryComparisonResponse> result = await mediator.Send(
                    new GetDashboardCategoryComparisonQuery(claims.GetUserId(), request.Month));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDashboardCategoryComparison))
            .WithTags("Dashboard")
            .WithSummary("Gets dashboard category comparison.")
            .WithDescription("Returns current and previous month spend by category for the authenticated user.")
            .RequireAuthorization(DashboardPolicyConsts.ReadPolicy)
            .Produces<DashboardCategoryComparisonResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetDashboardCategoryComparisonRequest
    {
        [FromQuery(Name = "month")]
        public string? Month { get; init; }
    }
}
