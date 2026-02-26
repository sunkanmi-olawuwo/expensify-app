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
using Expensify.Modules.Income.Application.Incomes.Query.GetMonthlySummary;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Incomes.Query;

public sealed class GetMonthlySummary : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.MonthlySummary, async (IMediator mediator, ClaimsPrincipal claims, string period) =>
            {
                Result<MonthlyIncomeSummaryResponse> result = await mediator.Send(new GetMonthlySummaryQuery(claims.GetUserId(), period));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("IncomeGetMonthlySummary")
            .WithTags("Income")
            .WithSummary("Gets monthly income summary.")
            .WithDescription("Returns total amount, count, and type totals for the selected period.")
            .RequireAuthorization(IncomePolicyConsts.ReadPolicy)
            .Produces<MonthlyIncomeSummaryResponse>(StatusCodes.Status200OK);
    }
}
