using System;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Command.CreateTimezone;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Admin.Command;

public sealed class CreateTimezone : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(RouteConsts.Timezones, async (IMediator mediator, CreateTimezoneBody body) =>
            {
                Result<TimezoneResponse> result = await mediator.Send(new CreateTimezoneCommand(
                    body.IanaId,
                    body.DisplayName,
                    body.IsActive,
                    body.IsDefault,
                    body.SortOrder));

                return result.Match(
                    response => Results.Created($"{RouteConsts.Timezones}/{Uri.EscapeDataString(response.IanaId)}", response),
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(CreateTimezone))
            .WithTags(nameof(Users))
            .WithSummary("Creates a timezone.")
            .WithDescription("Creates a new allowed timezone for the application.")
            .RequireAuthorization(UserPolicyConsts.ManagePreferenceCatalogPolicy)
            .Produces<TimezoneResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record CreateTimezoneBody(
        string IanaId,
        string DisplayName,
        bool IsActive,
        bool IsDefault,
        int SortOrder);
}
