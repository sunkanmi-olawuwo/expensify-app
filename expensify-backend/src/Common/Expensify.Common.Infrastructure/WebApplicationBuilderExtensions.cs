using System.Reflection;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Carter;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using Expensify.Api.OpenApi;
using Expensify.Common.Application;
using Expensify.Common.Application.OpenApi;
using Expensify.Common.Infrastructure.Util;
using Expensify.Common.Infrastructure.Versioning;

namespace Expensify.Common.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureService(this WebApplicationBuilder builder, IWebHostEnvironment environment)
    {
        builder.Services.ConfigureJson();
        builder.Services.AddCustomCors();
        builder.Services.AddVersioning();

        builder.Services.AddProblemDetails();
        builder.Services.AddSignalR().AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.UseMapster(environment.IsDevelopment())
         .AddSwagger(ApplicationConstants.ApplicationName, InfrastructureConfiguration.Versions)
         .AddSingleton<IConfigureOpenApiSettings, OpenApiSettingsBuilder>();

        return builder;
    }

    public static IServiceCollection AddCarterModules(
        this IServiceCollection services,
        params Type[] handlerAssemblyMarkerTypes)
    {
        // Use constructor that takes explicit catalog to avoid using DependencyContext.
        // This is not supported with single-file deployments (like efbundle).
        // https://github.com/CarterCommunity/Carter/issues/291

        Assembly[] assemblies = handlerAssemblyMarkerTypes.Select(t => t.Assembly).ToArray();
        var catalog = new DependencyContextAssemblyCatalog(assemblies);
        return services.AddCarter(catalog);
    }

    public static IServiceCollection AddMapster(
        this IServiceCollection services,
        params Type[] configurationAssemblyMarkerTypes)
    {
        Assembly[] assemblies = configurationAssemblyMarkerTypes.Select(t => t.Assembly).ToArray();
        services.AddSingleton(new MapsterRegistration(assemblies));
        return services;
    }

    public static IServiceCollection UseMapster(
        this IServiceCollection services,
        bool strictMode = false)
    {
        Assembly[] assemblies = services
            .Where(d => d.ServiceType == typeof(MapsterRegistration) && d is { IsKeyedService: false, ImplementationInstance: not null })
            .Select(d => d.ImplementationInstance as MapsterRegistration)
            .Cast<MapsterRegistration>()
            .SelectMany(d => d.Registrations)
            .ToArray();

        if (assemblies.Length > 0)
        {
            var config = new TypeAdapterConfig();
            config.Scan(assemblies);
            config.Compile();
            if (strictMode)
            {
                config.RequireExplicitMapping = true;
            }

            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();
        }

        return services;
    }


    internal static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name: ApplicationConstants.CorsPolicy,
                builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
        });
        return services;
    }

    internal static IServiceCollection ConfigureJson(this IServiceCollection services) =>
        services.Configure<JsonOptions>(
            options =>
            {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

    public static IServiceCollection AddSwagger(this IServiceCollection services, string name, IEnumerable<ApiVersion> apiVersions)
    {
        services.AddEndpointsApiExplorer();

        foreach (ApiVersion version in apiVersions)
        {
            services.AddOpenApiDocument((settings, provider) =>
            {
                string suffixedVersion = $"v{version.MajorVersion}";
                settings.Title = $"{name} API {suffixedVersion}";
                settings.DocumentName = suffixedVersion;
                settings.ApiGroupNames = [suffixedVersion];
                settings.Version = $"{version.MajorVersion}.{version.MinorVersion}";
                settings.SchemaSettings.SchemaNameGenerator = new CustomSchemaNameGenerator();
                settings.PostProcess = document =>
                {
                    string prefix = "/api/v" + version.MajorVersion;
                    foreach (KeyValuePair<string, OpenApiPathItem> pair in document.Paths.ToArray())
                    {
                        document.Paths.Remove(pair.Key);
                        document.Paths[pair.Key[prefix.Length..]] = pair.Value;
                    }
                };

                IEnumerable<IConfigureOpenApiSettings> additionalConfigurations = provider.GetServices<IConfigureOpenApiSettings>();
                foreach (IConfigureOpenApiSettings additional in additionalConfigurations)
                {
                    additional.ConfigureOpenApiSettings(version, settings);
                }
            });
        }

        return services;
    }
}
