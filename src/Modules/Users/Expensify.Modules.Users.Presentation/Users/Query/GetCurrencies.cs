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
using Expensify.Modules.Users.Application.Users.Query.GetCurrencies;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Users.Query;

public sealed class GetCurrencies : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.Currencies,
            async (IMediator mediator, ClaimsPrincipal claims, bool includeInactive = false) =>
            {
                if (includeInactive && !claims.HasClaim(UserPolicyConsts.ManagePreferenceCatalogPolicy, UserPolicyConsts.ManagePreferenceCatalogClaimValue))
                {
                    return Results.Forbid();
                }

                Result<IReadOnlyCollection<CurrencyResponse>> result =
                    await mediator.Send(new GetCurrenciesQuery(includeInactive));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetCurrencies))
            .WithTags(nameof(Users))
            .WithSummary("Gets allowed currencies.")
            .WithDescription("Returns allowed currencies for the application.")
            .RequireAuthorization(UserPolicyConsts.ReadPolicy)
            .Produces<IReadOnlyCollection<CurrencyResponse>>(StatusCodes.Status200OK);
    }
}
