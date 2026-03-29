using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Categories.Command.UpdateInvestmentCategory;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Presentation.Categories.Command;

public sealed class UpdateInvestmentCategory : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.AdminCategoryById, async (IMediator mediator, Guid categoryId, UpdateInvestmentCategoryRequest request) =>
            {
                Result<InvestmentCategoryResponse> result = await mediator.Send(new UpdateInvestmentCategoryCommand(categoryId, request.IsActive));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateInvestmentCategory))
            .WithTags("Investments")
            .WithSummary("Updates an investment category.")
            .WithDescription("Activates or deactivates a seeded investment category.")
            .RequireAuthorization(InvestmentPolicyConsts.AdminManageCategoriesPolicy)
            .Produces<InvestmentCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record UpdateInvestmentCategoryRequest(bool IsActive);
}
