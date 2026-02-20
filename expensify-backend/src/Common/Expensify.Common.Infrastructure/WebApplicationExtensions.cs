using System.Net.Mime;
using System.Text;
using Carter;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Expensify.Common.Application;
using Expensify.Common.Application.SignalR;
using Expensify.Common.Infrastructure.OpenApi;
using Expensify.Common.Infrastructure.SignalR;
using Serilog;

namespace Expensify.Common.Infrastructure;

public static class WebApplicationExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseCommonExceptionHandler();

        app.UseCors(ApplicationConstants.CorsPolicy);
        app.NewVersionedApi()
            .MapGroup("/api/v{version:apiVersion}")
            .MapCarter();

        if (!app.Environment.IsProduction())
        {
            app.MapSwagger();
            app.MapScalar(ApplicationConstants.ApplicationName);
        }

        app.MapHealthChecks("health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
        if (app.Services.GetService<ISignalrSubscriptionCache>() is null)
        {
            throw new Exception(
                $"Expected implementation of {nameof(ISignalrSubscriptionCache)} is not available in the DI container.");
        }
        app.MapHub<ExpensifyHub>("/notifications");
        return app;
    }

    private static string SanitizePathForLogging(string path) => path.Replace("\r", "").Replace("\n", "");

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
    private static void UseCommonExceptionHandler(this WebApplication app)
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                ILogger logger = context.RequestServices
                    .GetRequiredService<ILogger>()
                    .ForContext("RequestPath", context.Request.Path == PathString.Empty
                        ? SanitizePathForLogging(context.Request.PathBase.ToString())
                        : SanitizePathForLogging(context.Request.Path.ToString()));
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = MediaTypeNames.Text.Plain;

                string title = "An error occurred while processing your request.";
                string? detail = default;
                string type = "https://tools.ietf.org/html/rfc9110#section-15.6.1";

                if (context.RequestServices.GetService<IProblemDetailsService>() is
                    { } problemDetailsService)
                {
                    IExceptionHandlerFeature? exceptionHandlerFeature =
                        context.Features.Get<IExceptionHandlerFeature>();

                    Exception? exceptionType = exceptionHandlerFeature?.Error;
                    if (exceptionType != null)
                    {
                        if (exceptionType is BadHttpRequestException badRequest)
                        {
                            context.Response.StatusCode = badRequest.StatusCode;
                            title = "Bad Request";
                            type = "https://datatracker.ietf.org/doc/html/rfc9110#name-400-bad-request";
                            var builder = new StringBuilder();
                            Exception? exc = badRequest;
                            while (exc != null)
                            {
                                builder.AppendLine(exc.Message);
                                exc = exc.InnerException;
                            }
                            detail = builder.ToString();
                        }
                        else if (app.Environment.IsDevelopment() && exceptionType is Mapster.CompileException mappingException)
                        {
                            title = "Missing mapping";
                            var builder = new StringBuilder();
                            Exception? exc = mappingException;
                            while (exc != null)
                            {
                                builder.AppendLine(exc.Message);
                                exc = exc.InnerException;
                            }
                            detail = builder.ToString();
                        }

                        logger.Error(exceptionType, title);
                    }
                    else
                    {
                        logger.Error(title);
                    }

                    await problemDetailsService.WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context,
                        ProblemDetails =
                        {
                            Title = title,
                            Detail = detail,
                            Type = type
                        }
                    });
                }
                else
                {
                    logger.Error(title);
                }
            });
        });
    }
}
