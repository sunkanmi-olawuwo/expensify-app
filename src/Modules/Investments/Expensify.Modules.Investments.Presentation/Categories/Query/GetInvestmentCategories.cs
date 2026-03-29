using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Categories.Query.GetInvestmentCategories;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Categories.Query;

public sealed class GetInvestmentCategories : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Categories, async (IMediator mediator) =>
            {
                Result<IReadOnlyCollection<InvestmentCategoryResponse>> result =
                    await mediator.Send(new GetInvestmentCategoriesQuery(false));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetInvestmentCategories))
            .WithTags("Investments")
            .WithSummary("Gets active investment categories.")
            .WithDescription("Returns active investment categories for users.")
            .RequireAuthorization(InvestmentPolicyConsts.ReadPolicy)
            .Produces<IReadOnlyCollection<InvestmentCategoryResponse>>(StatusCodes.Status200OK);

        app.MapGet(RouteConsts.AdminCategories, async (IMediator mediator) =>
            {
                Result<IReadOnlyCollection<InvestmentCategoryResponse>> result =
                    await mediator.Send(new GetInvestmentCategoriesQuery(true));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("GetAllInvestmentCategories")
            .WithTags("Investments")
            .WithSummary("Gets all investment categories including inactive.")
            .WithDescription("Returns all investment categories for administrators.")
            .RequireAuthorization(InvestmentPolicyConsts.AdminManageCategoriesPolicy)
            .Produces<IReadOnlyCollection<InvestmentCategoryResponse>>(StatusCodes.Status200OK);
    }
}
