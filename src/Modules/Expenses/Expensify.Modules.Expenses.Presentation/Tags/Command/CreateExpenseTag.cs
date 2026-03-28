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
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Tags.Command;

public sealed class CreateExpenseTag : ICarterModule
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
            .WithName(nameof(CreateExpenseTag))
            .WithTags(nameof(Tags))
            .WithSummary("Creates a new expense tag.")
            .WithDescription("Creates a new expense tag for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseTagResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
