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
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Query.GetMonthlySummary;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Expenses.Query;

public sealed class GetMonthlySummary : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.MonthlySummary, async (IMediator mediator, ClaimsPrincipal claims, string period) =>
            {
                Result<MonthlyExpensesSummaryResponse> result = await mediator.Send(new GetMonthlySummaryQuery(claims.GetUserId(), period));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetMonthlySummary))
            .WithTags(nameof(Expenses))
            .WithSummary("Gets monthly expense summary.")
            .WithDescription("Returns total amount, count, and category totals for the selected period.")
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<MonthlyExpensesSummaryResponse>(StatusCodes.Status200OK);
    }
}
