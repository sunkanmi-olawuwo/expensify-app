using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Expensify.Common.Infrastructure.RateLimiting;

public static class RateLimitingRequestPartitioner
{
    private const string VersionedApiPathPrefix = "/api/v";

    public static bool IsWriteRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string path = context.Request.Path.Value ?? string.Empty;

        if (!path.StartsWith(VersionedApiPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
               context.Request.Method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
               context.Request.Method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase) ||
               context.Request.Method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAuthWriteRequest(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string path = context.Request.Path.Value ?? string.Empty;

        if (!path.StartsWith(VersionedApiPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!context.Request.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.EndsWith("/users/login", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/users/register", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("/users/refresh", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetWritePartitionKey(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? userId = context.User.FindFirstValue("userid");
        if (Guid.TryParse(userId, out Guid parsedUserId))
        {
            return $"user:{parsedUserId:N}";
        }

        return $"ip:{GetClientIp(context)}";
    }

    public static string GetAuthPartitionKey(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return $"ip:{GetClientIp(context)}";
    }

    private static string GetClientIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
