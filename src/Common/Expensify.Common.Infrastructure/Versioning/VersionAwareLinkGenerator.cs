using Carter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Expensify.Common.Application.Versioning;

namespace Expensify.Common.Infrastructure.Versioning;

public class VersionAwareLinkGenerator(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    : IVersionAwareLinkGenerator
{
    public string? GetEndpointPath<TEndpoint>(object? values = null)
        where TEndpoint : ICarterModule =>
        linkGenerator.GetPathByName(httpContextAccessor.HttpContext!, typeof(TEndpoint).Name, values);
}
