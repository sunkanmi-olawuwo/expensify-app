using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Admin.Command.DeleteUser;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Admin.Command;

public class DeleteUser : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete($"{RouteConsts.DeleteUser}",
           async (IMediator mediator, [AsParameters] DeleteUserCommand command) =>
           {
               Result result = await mediator.Send(command);
               return result.Match(Results.NoContent, ApiResults.Problem);
           })
           .HasApiVersion(InfrastructureConfiguration.V1)
           .WithName(nameof(DeleteUser))
           .WithTags(nameof(Users))
           .WithDescription("Deletes a user.")
           .WithSummary("Deletes a user.")
           .RequireAuthorization(UserPolicyConsts.DeletePolicy)
           .Produces(StatusCodes.Status204NoContent);
    }
}
