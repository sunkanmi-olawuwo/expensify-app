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
using Expensify.Modules.Investments.Application.Accounts.Query.GetInvestmentAccount;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Accounts.Query;

public sealed class GetInvestmentAccount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.InvestmentById, async (IMediator mediator, ClaimsPrincipal claims, Guid investmentId) =>
            {
                Result<InvestmentAccountResponse> result = await mediator.Send(new GetInvestmentAccountQuery(claims.GetUserId(), investmentId));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetInvestmentAccount))
            .WithTags("Investments")
            .WithSummary("Gets an investment account.")
            .WithDescription("Returns a single investment account for the current user.")
            .RequireAuthorization(InvestmentPolicyConsts.ReadPolicy)
            .Produces<InvestmentAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
