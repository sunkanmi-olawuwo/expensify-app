using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Expensify.Common.Infrastructure;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Modules.Dashboard.Infrastructure.Policies;

namespace Expensify.Modules.Dashboard.Infrastructure;

public static class DashboardModule
{
#pragma warning disable IDE0060 // Keeping parameter for consistency with other module registration methods
    public static IServiceCollection AddDashboardModule(this IServiceCollection services, IConfiguration configuration)
#pragma warning restore IDE0060
    {
        Type[] configurationAssemblyMarkerTypes =
        [
            typeof(Application.AssemblyReference),
            typeof(DashboardModule),
            typeof(Presentation.AssemblyReference)
        ];

        services.AddMapster(configurationAssemblyMarkerTypes);
        services.AddCarterModules(typeof(Presentation.AssemblyReference));
        services.AddSingleton<IPolicyFactory, DashboardPolicyFactory>();

        return services;
    }
}
