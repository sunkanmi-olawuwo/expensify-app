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
using Expensify.Modules.Income.Application.Incomes.Command.UpdateIncome;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Incomes.Command;

public sealed class UpdateIncome : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.IncomeById, async (IMediator mediator, ClaimsPrincipal claims, Guid incomeId, UpdateIncomeRequest request) =>
            {
                Result<IncomeResponse> result = await mediator.Send(
                    new UpdateIncomeCommand(
                        claims.GetUserId(),
                        incomeId,
                        request.Amount,
                        request.Currency,
                        request.Date,
                        request.Source,
                        request.Type,
                        request.Note));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateIncome))
            .WithTags("Income")
            .WithSummary("Updates an income.")
            .WithDescription("Updates an existing income record for the current user.")
            .RequireAuthorization(IncomePolicyConsts.WritePolicy)
            .Produces<IncomeResponse>(StatusCodes.Status200OK);
    }

    public sealed record UpdateIncomeRequest(
        decimal Amount,
        string Currency,
        DateOnly Date,
        string? Source,
        IncomeType Type,
        string? Note);
}
