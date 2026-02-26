using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Admin.Query.GetUserMonthlySummary;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Presentation.Admin.Query;

public sealed class GetUserMonthlySummary : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.AdminMonthlySummary, async (IMediator mediator, Guid userId, string period) =>
            {
                Result<MonthlyIncomeSummaryResponse> result = await mediator.Send(new GetUserMonthlySummaryQuery(userId, period));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("IncomeGetUserMonthlySummary")
            .WithTags("Income")
            .WithSummary("Gets monthly income summary for admins.")
            .WithDescription("Returns monthly income summary for a selected user.")
            .RequireAuthorization(IncomePolicyConsts.AdminReadPolicy)
            .Produces<MonthlyIncomeSummaryResponse>(StatusCodes.Status200OK);
    }
}
