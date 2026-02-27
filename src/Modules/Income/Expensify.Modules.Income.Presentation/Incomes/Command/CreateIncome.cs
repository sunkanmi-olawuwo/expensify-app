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
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Incomes.Command.CreateIncome;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Incomes.Command;

public sealed class CreateIncome : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Income, async (IMediator mediator, ClaimsPrincipal claims, CreateIncomeRequest request) =>
            {
                Result<IncomeResponse> result = await mediator.Send(
                    new CreateIncomeCommand(
                        claims.GetUserId(),
                        request.Amount,
                        request.Currency,
                        request.Date,
                        request.Source,
                        request.Type,
                        request.Note));

                return result.Match(
                    response => Results.Created($"{RouteConsts.Income}/{response.Id}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(CreateIncome))
            .WithTags("Income")
            .WithSummary("Creates a new income.")
            .WithDescription("Creates a new income record for the current user.")
            .RequireAuthorization(IncomePolicyConsts.WritePolicy)
            .Produces<IncomeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record CreateIncomeRequest(
        decimal Amount,
        string Currency,
        DateOnly Date,
        string? Source,
        IncomeType Type,
        string? Note);
}
