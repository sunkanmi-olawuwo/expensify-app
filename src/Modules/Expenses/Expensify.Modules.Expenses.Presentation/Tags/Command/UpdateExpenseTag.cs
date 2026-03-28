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
using Expensify.Modules.Expenses.Application.Tags.Command.UpdateExpenseTag;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Tags.Command;

public sealed class UpdateExpenseTag : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.TagById, async (IMediator mediator, ClaimsPrincipal claims, Guid tagId, TagBody body) =>
            {
                Result<ExpenseTagResponse> result = await mediator.Send(new UpdateExpenseTagCommand(claims.GetUserId(), tagId, body.Name));
                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateExpenseTag))
            .WithTags(nameof(Tags))
            .WithSummary("Updates an expense tag.")
            .WithDescription("Updates an existing expense tag for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.WritePolicy)
            .Produces<ExpenseTagResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
