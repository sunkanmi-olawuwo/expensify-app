using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using NSwag;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors.Security;
using Expensify.Common.Application.OpenApi;

namespace Expensify.Api.OpenApi;

public class OpenApiSettingsBuilder : IConfigureOpenApiSettings
{
    public void ConfigureOpenApiSettings(ApiVersion version, AspNetCoreOpenApiDocumentGeneratorSettings settings)
    {
        settings.AddSecurity("Bearer", [], new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = OpenApiSecurityApiKeyLocation.Header,
            Description = "Enter JWT Bearer token in the format: 'Bearer {token}'"
        });

        settings.OperationProcessors.Add(
            new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
    }
}
