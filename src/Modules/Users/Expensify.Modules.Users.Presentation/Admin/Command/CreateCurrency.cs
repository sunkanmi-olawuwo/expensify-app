using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Command.CreateCurrency;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Admin.Command;

public sealed class CreateCurrency : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Currencies, async (IMediator mediator, CreateCurrencyBody body) =>
            {
                Result<CurrencyResponse> result = await mediator.Send(new CreateCurrencyCommand(
                    body.Code,
                    body.Name,
                    body.Symbol,
                    body.MinorUnit,
                    body.IsActive,
                    body.IsDefault,
                    body.SortOrder));

                return result.Match(
                    response => Results.Created($"{RouteConsts.Currencies}/{response.Code}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(CreateCurrency))
            .WithTags(nameof(Users))
            .WithSummary("Creates a currency.")
            .WithDescription("Creates a new allowed currency for the application.")
            .RequireAuthorization(UserPolicyConsts.ManagePreferenceCatalogPolicy)
            .Produces<CurrencyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record CreateCurrencyBody(
        string Code,
        string Name,
        string Symbol,
        int MinorUnit,
        bool IsActive,
        bool IsDefault,
        int SortOrder);
}
