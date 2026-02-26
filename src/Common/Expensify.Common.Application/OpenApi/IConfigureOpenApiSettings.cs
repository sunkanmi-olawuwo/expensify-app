using Asp.Versioning;
using NSwag.Generation.AspNetCore;

namespace Expensify.Common.Application.OpenApi;

public interface IConfigureOpenApiSettings
{
    void ConfigureOpenApiSettings(ApiVersion version, AspNetCoreOpenApiDocumentGeneratorSettings settings);
}

