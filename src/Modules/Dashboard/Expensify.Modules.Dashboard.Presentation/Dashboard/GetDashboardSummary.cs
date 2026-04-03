using System.Security.Claims;
using Carter;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Dashboard.Application.Dashboard.GetDashboardSummary;
using Expensify.Modules.Dashboard.Domain.Policies;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Expensify.Modules.Dashboard.Presentation.Dashboard;

public sealed class GetDashboardSummary : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Summary, async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result<DashboardSummaryResponse> result = await mediator.Send(new GetDashboardSummaryQuery(claims.GetUserId()));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDashboardSummary))
            .WithTags("Dashboard")
            .WithSummary("Gets dashboard summary.")
            .WithDescription("Returns the aggregated dashboard payload for the authenticated user.")
            .RequireAuthorization(DashboardPolicyConsts.ReadPolicy)
            .Produces<DashboardSummaryResponse>(StatusCodes.Status200OK);
    }
}
