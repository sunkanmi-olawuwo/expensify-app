using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Admin.Query.GetUserMonthlySummary;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Admin.Query;

public sealed class GetUserMonthlySummary : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.AdminMonthlySummary, async (IMediator mediator, Guid userId, string period) =>
            {
                Result<MonthlyExpensesSummaryResponse> result = await mediator.Send(new GetUserMonthlySummaryQuery(userId, period));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetUserMonthlySummary))
            .WithTags(nameof(Expenses))
            .WithSummary("Gets monthly expense summary for a specific user (admin).")
            .WithDescription("Admin-only endpoint to read monthly summary for a given user.")
            .RequireAuthorization(ExpensePolicyConsts.AdminReadPolicy)
            .Produces<MonthlyExpensesSummaryResponse>(StatusCodes.Status200OK);
    }
}
