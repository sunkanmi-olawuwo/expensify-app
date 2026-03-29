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
using Expensify.Modules.Investments.Application.Contributions.Command.CreateInvestmentContribution;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Contributions.Command;

public sealed class CreateInvestmentContribution : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Contributions, async (IMediator mediator, ClaimsPrincipal claims, Guid investmentId, CreateInvestmentContributionRequest request) =>
            {
                Result<InvestmentContributionResponse> result = await mediator.Send(
                    new CreateInvestmentContributionCommand(
                        claims.GetUserId(),
                        investmentId,
                        request.Amount,
                        request.Date,
                        request.Notes));

                return result.Match(
                    response => Results.Created($"{RouteConsts.Investments}/{response.InvestmentId}/contributions/{response.Id}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(CreateInvestmentContribution))
            .WithTags("Investments")
            .WithSummary("Creates an investment contribution.")
            .WithDescription("Adds a contribution to an investment account for the current user.")
            .RequireAuthorization(InvestmentPolicyConsts.WritePolicy)
            .Produces<InvestmentContributionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record CreateInvestmentContributionRequest(decimal Amount, DateTimeOffset Date, string? Notes);
}
