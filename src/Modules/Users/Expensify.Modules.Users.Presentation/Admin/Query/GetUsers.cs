using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Globalization;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure;
using Expensify.Common.Presentation.Results;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Query.GetUsers;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Presentation.Admin.Query;

public class GetUsers : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(RouteConsts.GetUsers,
            async (IMediator mediator, HttpContext httpContext, [AsParameters] GetUsersQuery query) =>
            {
                Result<GetUsersResponse> result = await mediator.Send(query);

                return result.Match(response =>
                    {
                        httpContext.Response.Headers.Append("X-Pagination-CurrentPage", response.CurentPage.ToString(CultureInfo.InvariantCulture));
                        httpContext.Response.Headers.Append("X-Pagination-PageSize", response.PageSize.ToString(CultureInfo.InvariantCulture));
                        httpContext.Response.Headers.Append("X-Pagination-TotalCount", response.TotalCount.ToString(CultureInfo.InvariantCulture));
                        httpContext.Response.Headers.Append("X-Pagination-TotalPages", response.TotalPages.ToString(CultureInfo.InvariantCulture));

                        return Results.Ok(response);
                    },
                    ApiResults.Problem);
            })
            .HasApiVersion(InfrastructureConfiguration.V1)
            .WithName(nameof(GetUsers))
            .WithTags(nameof(Users))
            .WithDescription("Gets a paginated list of users.")
            .WithSummary("Gets users.")
            .RequireAuthorization(UserPolicyConsts.ReadAllPolicy)
            .Produces<GetUsersResponse>(StatusCodes.Status200OK);
    }
}
