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
using Expensify.Modules.Expenses.Application.Categories.Command.CreateExpenseCategory;
using Expensify.Modules.Expenses.Application.Categories.Command.DeleteExpenseCategory;
using Expensify.Modules.Expenses.Application.Categories.Command.UpdateExpenseCategory;
using Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategories;
using Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategory;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Categories;

public sealed class ExpenseCategoriesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Categories, async (IMediator mediator, ClaimsPrincipal claims, CategoryBody body) =>
            {
                Result<ExpenseCategoryResponse> result = await mediator.Send(new CreateExpenseCategoryCommand(claims.GetUserId(), body.Name));
                return result.Match(
                    response => Results.Created($"{RouteConsts.Categories}/{response.Id}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("CreateExpenseCategory")
            .WithTags(nameof(Categories))
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        app.MapGet(RouteConsts.Categories, async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result<IReadOnlyCollection<ExpenseCategoryResponse>> result = await mediator.Send(new GetExpenseCategoriesQuery(claims.GetUserId()));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("GetExpenseCategories")
            .WithTags(nameof(Categories))
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<IReadOnlyCollection<ExpenseCategoryResponse>>(StatusCodes.Status200OK);

        app.MapGet(RouteConsts.CategoryById, async (IMediator mediator, ClaimsPrincipal claims, Guid categoryId) =>
            {
                Result<ExpenseCategoryResponse> result = await mediator.Send(new GetExpenseCategoryQuery(claims.GetUserId(), categoryId));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("GetExpenseCategory")
            .WithTags(nameof(Categories))
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<ExpenseCategoryResponse>(StatusCodes.Status200OK);

        app.MapPut(RouteConsts.CategoryById, async (IMediator mediator, ClaimsPrincipal claims, Guid categoryId, CategoryBody body) =>
            {
                Result<ExpenseCategoryResponse> result = await mediator.Send(new UpdateExpenseCategoryCommand(claims.GetUserId(), categoryId, body.Name));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("UpdateExpenseCategory")
            .WithTags(nameof(Categories))
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseCategoryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        app.MapDelete(RouteConsts.CategoryById, async (IMediator mediator, ClaimsPrincipal claims, Guid categoryId) =>
            {
                Result result = await mediator.Send(new DeleteExpenseCategoryCommand(claims.GetUserId(), categoryId));
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("DeleteExpenseCategory")
            .WithTags(nameof(Categories))
            .RequireAuthorization(ExpensePolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record CategoryBody(string Name);
}
