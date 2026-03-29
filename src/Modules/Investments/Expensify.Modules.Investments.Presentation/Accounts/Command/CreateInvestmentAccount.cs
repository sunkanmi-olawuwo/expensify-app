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
using Expensify.Modules.Investments.Application.Accounts.Command.CreateInvestmentAccount;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Accounts.Command;

public sealed class CreateInvestmentAccount : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Investments, async (IMediator mediator, ClaimsPrincipal claims, CreateInvestmentAccountRequest request) =>
            {
                Result<InvestmentAccountResponse> result = await mediator.Send(new CreateInvestmentAccountCommand(
                    claims.GetUserId(),
                    request.Name,
                    request.Provider,
                    request.CategoryId,
                    request.Currency,
                    request.InterestRate,
                    request.MaturityDate,
                    request.CurrentBalance,
                    request.Notes));

                return result.Match(
                    response => Results.Created($"{RouteConsts.Investments}/{response.Id}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(CreateInvestmentAccount))
            .WithTags("Investments")
            .WithSummary("Creates a new investment account.")
            .WithDescription("Creates a new investment account for the current user.")
            .RequireAuthorization(InvestmentPolicyConsts.WritePolicy)
            .Produces<InvestmentAccountResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record CreateInvestmentAccountRequest(
        string Name,
        string? Provider,
        Guid CategoryId,
        string Currency,
        decimal? InterestRate,
        DateTimeOffset? MaturityDate,
        decimal CurrentBalance,
        string? Notes);
}
