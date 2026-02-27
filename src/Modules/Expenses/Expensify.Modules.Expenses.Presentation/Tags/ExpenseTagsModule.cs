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
using Expensify.Modules.Expenses.Application.Tags.Command.CreateExpenseTag;
using Expensify.Modules.Expenses.Application.Tags.Command.DeleteExpenseTag;
using Expensify.Modules.Expenses.Application.Tags.Command.UpdateExpenseTag;
using Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTag;
using Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTags;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Tags;

public sealed class ExpenseTagsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Tags, async (IMediator mediator, ClaimsPrincipal claims, TagBody body) =>
            {
                Result<ExpenseTagResponse> result = await mediator.Send(new CreateExpenseTagCommand(claims.GetUserId(), body.Name));
                return result.Match(
                    response => Results.Created($"{RouteConsts.Tags}/{response.Id}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("CreateExpenseTag")
            .WithTags(nameof(Tags))
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseTagResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        app.MapGet(RouteConsts.Tags, async (IMediator mediator, ClaimsPrincipal claims) =>
            {
                Result<IReadOnlyCollection<ExpenseTagResponse>> result = await mediator.Send(new GetExpenseTagsQuery(claims.GetUserId()));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("GetExpenseTags")
            .WithTags(nameof(Tags))
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<IReadOnlyCollection<ExpenseTagResponse>>(StatusCodes.Status200OK);

        app.MapGet(RouteConsts.TagById, async (IMediator mediator, ClaimsPrincipal claims, Guid tagId) =>
            {
                Result<ExpenseTagResponse> result = await mediator.Send(new GetExpenseTagQuery(claims.GetUserId(), tagId));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("GetExpenseTag")
            .WithTags(nameof(Tags))
            .RequireAuthorization(ExpensePolicyConsts.ReadPolicy)
            .Produces<ExpenseTagResponse>(StatusCodes.Status200OK);

        app.MapPut(RouteConsts.TagById, async (IMediator mediator, ClaimsPrincipal claims, Guid tagId, TagBody body) =>
            {
                Result<ExpenseTagResponse> result = await mediator.Send(new UpdateExpenseTagCommand(claims.GetUserId(), tagId, body.Name));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("UpdateExpenseTag")
            .WithTags(nameof(Tags))
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseTagResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        app.MapDelete(RouteConsts.TagById, async (IMediator mediator, ClaimsPrincipal claims, Guid tagId) =>
            {
                Result result = await mediator.Send(new DeleteExpenseTagCommand(claims.GetUserId(), tagId));
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName("DeleteExpenseTag")
            .WithTags(nameof(Tags))
            .RequireAuthorization(ExpensePolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record TagBody(string Name);
}
