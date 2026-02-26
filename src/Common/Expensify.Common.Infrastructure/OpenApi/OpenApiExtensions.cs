using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;

namespace Expensify.Common.Infrastructure.OpenApi;

public static class OpenApiExtensions
{
    public static IApplicationBuilder MapSwagger(this IApplicationBuilder app)
    {
        app.UseOpenApi(settings =>
        {
            settings.Path = "/api/swagger/{documentName}/swagger.json";
            settings.PostProcess = (document, _) =>
            {
                // Update server URL for Swagger UI to include version.
                string prefix = "/api/v" + document.Info.Version.Split('.')[0];
                document.Servers.First().Url += prefix;
            };
        });
        app.UseSwaggerUi(settings =>
        {
            // /api/index.html
            settings.Path = "/api";
            settings.TransformToExternalPath = (url, _) => url.EndsWith("swagger.json", StringComparison.Ordinal) ? $"/api{url}" : url;
        });

        return app;
    }

    public static WebApplication MapScalar(
        this WebApplication app,
        string title = "Expensify.API",
        ApiVersion? defaultVersion = null)
    {
        IApiVersionDescriptionProvider provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.MapScalarApiReference(option =>
        {
            option.Title = title;

            var sortedVersions = provider.ApiVersionDescriptions
                .OrderBy(v => v.ApiVersion)
                .ToList();

            foreach (ApiVersionDescription? description in sortedVersions)
            {
                string versionName = description.GroupName;
                string versionNumber = description.ApiVersion.ToString();
                string displayName = $"{title} -- {versionNumber}";

                bool isDefault = defaultVersion is not null
                    && description.ApiVersion.Equals(defaultVersion);

                option.AddDocument(versionName, displayName, $"/api/swagger/{versionName}/swagger.json", isDefault);
            }
        });

        app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
        return app;
    }
}
