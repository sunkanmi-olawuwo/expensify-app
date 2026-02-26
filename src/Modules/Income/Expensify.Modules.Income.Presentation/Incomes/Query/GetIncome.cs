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
using Expensify.Modules.Income.Application.Incomes.Query.GetIncome;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Incomes.Query;

public sealed class GetIncome : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.IncomeById, async (IMediator mediator, ClaimsPrincipal claims, Guid incomeId) =>
            {
                Result<IncomeResponse> result = await mediator.Send(new GetIncomeQuery(claims.GetUserId(), incomeId));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetIncome))
            .WithTags("Income")
            .WithSummary("Gets an income by id.")
            .WithDescription("Returns an income record by identifier for the current user.")
            .RequireAuthorization(IncomePolicyConsts.ReadPolicy)
            .Produces<IncomeResponse>(StatusCodes.Status200OK);
    }
}
