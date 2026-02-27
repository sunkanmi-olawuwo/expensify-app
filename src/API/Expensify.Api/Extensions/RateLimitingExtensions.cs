using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Expensify.Common.Infrastructure.RateLimiting;

namespace Expensify.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        RateLimitingOptions options = new();
        configuration.GetSection(RateLimitingOptions.SectionName).Bind(options);

        services.Configure<ForwardedHeadersOptions>(forwardedHeadersOptions =>
        {
            // Accept client IP from trusted proxies only.
            forwardedHeadersOptions.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
        });

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";
                var problemDetails = new ProblemDetails
                {
                    Title = "RateLimit.Exceeded",
                    Detail = "Too many requests. Please retry after a short delay.",
                    Status = StatusCodes.Status429TooManyRequests,
                    Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.9"
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            };

            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!RateLimitingRequestPartitioner.IsWriteRequest(httpContext) ||
                    RateLimitingRequestPartitioner.IsAuthWriteRequest(httpContext))
                {
                    return RateLimitPartition.GetNoLimiter("non-limited");
                }

                string key = RateLimitingRequestPartitioner.GetWritePartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    key,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.Write.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.Write.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = options.Write.QueueLimit,
                        AutoReplenishment = true
                    });
            });

            rateLimiterOptions.AddPolicy(RateLimitingPolicyNames.AuthPolicy, httpContext =>
            {
                string key = RateLimitingRequestPartitioner.GetAuthPartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    key,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.Auth.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.Auth.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = options.Auth.QueueLimit,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }
}
