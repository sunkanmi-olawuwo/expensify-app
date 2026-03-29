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
using Expensify.Modules.Investments.Application.Accounts.Command.UpdateInvestmentAccount;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Accounts.Command;

public sealed class UpdateInvestmentAccount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.InvestmentById, async (IMediator mediator, ClaimsPrincipal claims, Guid investmentId, UpdateInvestmentAccountRequest request) =>
            {
                Result<InvestmentAccountResponse> result = await mediator.Send(new UpdateInvestmentAccountCommand(
                    claims.GetUserId(),
                    investmentId,
                    request.Name,
                    request.Provider,
                    request.CategoryId,
                    request.Currency,
                    request.InterestRate,
                    request.MaturityDate,
                    request.CurrentBalance,
                    request.Notes));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateInvestmentAccount))
            .WithTags("Investments")
            .WithSummary("Updates an investment account.")
            .WithDescription("Updates an existing investment account for the current user.")
            .RequireAuthorization(InvestmentPolicyConsts.WritePolicy)
            .Produces<InvestmentAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    public sealed record UpdateInvestmentAccountRequest(
        string Name,
        string? Provider,
        Guid CategoryId,
        string Currency,
        decimal? InterestRate,
        DateTimeOffset? MaturityDate,
        decimal CurrentBalance,
        string? Notes);
}
