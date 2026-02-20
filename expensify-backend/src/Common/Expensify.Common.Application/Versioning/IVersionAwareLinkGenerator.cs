using Carter;

namespace Expensify.Common.Application.Versioning;

public interface IVersionAwareLinkGenerator
{
    string? GetEndpointPath<TEndpoint>(object? values = null)
        where TEndpoint : ICarterModule;
}
