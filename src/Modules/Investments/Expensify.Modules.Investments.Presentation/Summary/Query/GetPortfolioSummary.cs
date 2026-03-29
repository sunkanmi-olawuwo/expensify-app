using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Summary.Query.GetPortfolioSummary;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Summary.Query;

public sealed class GetPortfolioSummary : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Summary, async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result<PortfolioSummaryResponse> result = await mediator.Send(new GetPortfolioSummaryQuery(claims.GetUserId()));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetPortfolioSummary))
            .WithTags("Investments")
            .WithSummary("Gets portfolio summary.")
            .WithDescription("Returns aggregate contribution and performance metrics for the current user.")
            .RequireAuthorization(InvestmentPolicyConsts.ReadPolicy)
            .Produces<PortfolioSummaryResponse>(StatusCodes.Status200OK);
    }
}
