using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Command.UpdateCurrency;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Admin.Command;

public sealed class UpdateCurrency : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.CurrencyByCode, async (IMediator mediator, string code, UpdateCurrencyBody body) =>
            {
                Result<CurrencyResponse> result = await mediator.Send(new UpdateCurrencyCommand(
                    code,
                    body.Name,
                    body.Symbol,
                    body.MinorUnit,
                    body.IsActive,
                    body.IsDefault,
                    body.SortOrder));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateCurrency))
            .WithTags(nameof(Users))
            .WithSummary("Updates a currency.")
            .WithDescription("Updates an allowed currency for the application.")
            .RequireAuthorization(UserPolicyConsts.ManagePreferenceCatalogPolicy)
            .Produces<CurrencyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record UpdateCurrencyBody(
        string Name,
        string Symbol,
        int MinorUnit,
        bool IsActive,
        bool IsDefault,
        int SortOrder);
}
