using System.IdentityModel.Tokens.Jwt;
using Expensify.Common.Application.Caching;
using Expensify.Modules.Users.Domain.Tokens;

namespace Expensify.Api.Middleware;

public class CheckRevocatedTokensMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the class
    /// </summary>
    public CheckRevocatedTokensMiddleware(RequestDelegate next, ICacheService cacheService)
    {
        _next = next;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Invokes middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        string requestPath = context.Request.Path.Value ?? string.Empty;

        // Skip login and refresh URLs
        if (requestPath.EndsWith("/users/login", StringComparison.OrdinalIgnoreCase)
            || requestPath.EndsWith("/users/refresh", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip users without a role
        System.Security.Claims.Claim? jwtId = context.User.FindFirst(JwtRegisteredClaimNames.Jti);
        System.Security.Claims.Claim? role = context.User.FindFirst("role");
        if (jwtId is null || role is null)
        {
            await _next(context);
            return;
        }

        // Check if current JWT token of user is in revocation list
        RevocatedTokenType? revocationType = await _cacheService.GetAsync<RevocatedTokenType?>(jwtId.Value);
        if (revocationType.HasValue)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }
}
