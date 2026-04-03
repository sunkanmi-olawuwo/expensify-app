using System.Security.Claims;
using Carter;
using Carter.ModelBinding;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardTopCategories;
using Expensify.Modules.Dashboard.Domain.Policies;
using Expensify.Modules.Dashboard.Presentation.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Expensify.Modules.Dashboard.Presentation.Dashboard.Analytics;

public sealed class GetDashboardTopCategories : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.TopCategories, async (IMediator mediator, ClaimsPrincipal claims, [AsParameters] GetDashboardTopCategoriesRequest request) =>
            {
                Result<DashboardTopCategoriesResponse> result = await mediator.Send(
                    new GetDashboardTopCategoriesQuery(claims.GetUserId(), request.Months, request.Limit));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetDashboardTopCategories))
            .WithTags("Dashboard")
            .WithSummary("Gets dashboard top categories.")
            .WithDescription("Returns the highest-spend categories for the authenticated user over the selected window.")
            .RequireAuthorization(DashboardPolicyConsts.ReadPolicy)
            .Produces<DashboardTopCategoriesResponse>(StatusCodes.Status200OK);
    }

    public sealed class GetDashboardTopCategoriesRequest
    {
        [FromQuery(Name = "months")]
        public int Months { get; init; } = 3;

        [FromQuery(Name = "limit")]
        public int Limit { get; init; } = 5;
    }
}
