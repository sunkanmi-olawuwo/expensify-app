using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Command.UpdateTimezone;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Admin.Command;

public sealed class UpdateTimezone : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut(RouteConsts.TimezoneById, async (IMediator mediator, string ianaId, UpdateTimezoneBody body) =>
            {
                string decodedIanaId = Uri.UnescapeDataString(ianaId);

                Result<TimezoneResponse> result = await mediator.Send(new UpdateTimezoneCommand(
                    decodedIanaId,
                    body.DisplayName,
                    body.IsActive,
                    body.IsDefault,
                    body.SortOrder));

                return result.Match(Results.Ok, ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(UpdateTimezone))
            .WithTags(nameof(Users))
            .WithSummary("Updates a timezone.")
            .WithDescription("Updates an allowed timezone for the application.")
            .RequireAuthorization(UserPolicyConsts.ManagePreferenceCatalogPolicy)
            .Produces<TimezoneResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    public sealed record UpdateTimezoneBody(
        string DisplayName,
        bool IsActive,
        bool IsDefault,
        int SortOrder);
}
