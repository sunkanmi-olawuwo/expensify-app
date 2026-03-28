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
using Expensify.Modules.Expenses.Application.Tags.Command.DeleteExpenseTag;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Presentation.Tags.Command;

public sealed class DeleteExpenseTag : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete(RouteConsts.TagById, async (IMediator mediator, ClaimsPrincipal claims, Guid tagId) =>
            {
                Result result = await mediator.Send(new DeleteExpenseTagCommand(claims.GetUserId(), tagId));
                return result.Match(Results.NoContent, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(DeleteExpenseTag))
            .WithTags(nameof(Tags))
            .WithSummary("Deletes an expense tag.")
            .WithDescription("Deletes an existing expense tag for the current user.")
            .RequireAuthorization(ExpensePolicyConsts.DeletePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }
}
