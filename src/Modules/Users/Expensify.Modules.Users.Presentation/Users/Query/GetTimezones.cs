using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Users.Query.GetTimezones;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Users.Query;

public sealed class GetTimezones : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Timezones,
            async (IMediator mediator, ClaimsPrincipal claims, bool includeInactive = false) =>
            {
                if (includeInactive && !claims.HasClaim(UserPolicyConsts.ManagePreferenceCatalogPolicy, UserPolicyConsts.ManagePreferenceCatalogClaimValue))
                {
                    return Results.Forbid();
                }

                Result<IReadOnlyCollection<TimezoneResponse>> result =
                    await mediator.Send(new GetTimezonesQuery(includeInactive));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetTimezones))
            .WithTags(nameof(Users))
            .WithSummary("Gets allowed timezones.")
            .WithDescription("Returns allowed timezones for the application.")
            .RequireAuthorization(UserPolicyConsts.ReadPolicy)
            .Produces<IReadOnlyCollection<TimezoneResponse>>(StatusCodes.Status200OK);
    }
}
