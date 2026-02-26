using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Expensify.Common.Infrastructure.Authorization.Policies;

namespace Expensify.Common.Infrastructure.Authorization;

internal static class AuthorizationExtensions
{
    internal static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<AuthorizationOptions>, AuthorizationConfigureOptions>();
        services.AddAuthorization();
      
        return services;
    }
}
