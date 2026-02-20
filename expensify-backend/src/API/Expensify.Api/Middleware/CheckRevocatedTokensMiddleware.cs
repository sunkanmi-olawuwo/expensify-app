using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
        // Skip login and refresh URLs
        if (context.Request.Path.StartsWithSegments("/login", StringComparison.Ordinal)
            || context.Request.Path.StartsWithSegments("/refresh", StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        // Skip users without a role
        Claim? jwtId = context.User.FindFirst(JwtRegisteredClaimNames.Jti);
        Claim? role = context.User.FindFirst(ClaimTypes.Role);
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
