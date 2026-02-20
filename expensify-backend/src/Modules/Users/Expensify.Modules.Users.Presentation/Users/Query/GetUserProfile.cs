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
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Query;
using Expensify.Modules.Users.Application.Users.Query.GetUser;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Users.Query;

public class GetUserProfile : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet($"{RouteConsts.UserProfile}",
           async (IMediator mediator, ClaimsPrincipal claims, [AsParameters] GetUserQuery query) =>
           {
               Result<GetUserResponse> result = await mediator.Send(new GetUserQuery(claims.GetUserId()));

               return result.Match(Results.Ok, ApiResults.Problem);
           })
           .HasApiVersion(InfrastructureConfiguration.V1)
           .WithName(nameof(GetUserProfile))
           .WithTags(nameof(Users))
           .WithDescription("Gets a user profile by the domain user id.")
           .WithSummary("Gets a user profile by id.")
           .RequireAuthorization(UserPolicyConsts.ReadPolicy)
           .Produces<GetUserResponse>(StatusCodes.Status200OK);
    }
}
