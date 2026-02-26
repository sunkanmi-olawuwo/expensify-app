using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Expensify.Common.Application.Versioning;

namespace Expensify.Common.Infrastructure.Versioning;

internal static class VersioningExtensions
{
    public static IServiceCollection AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddSingleton<IVersionAwareLinkGenerator, VersionAwareLinkGenerator>();

        return services;
    }
}
