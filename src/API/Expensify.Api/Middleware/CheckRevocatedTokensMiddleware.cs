using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Expensify.Common.Application.Caching;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Tokens;

namespace Expensify.Api.Middleware;

public class CheckRevocatedTokensMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the class
    /// </summary>
    public CheckRevocatedTokensMiddleware(
        RequestDelegate next,
        ICacheService cacheService)
    {
        _next = next;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Invokes middleware
    /// </summary>
    public async Task InvokeAsync(HttpContext context, IIdentitySecurityStampValidator securityStampValidator)
    {
        string requestPath = context.Request.Path.Value ?? string.Empty;

        // Skip login and refresh URLs
        if (requestPath.EndsWith("/users/login", StringComparison.OrdinalIgnoreCase)
            || requestPath.EndsWith("/users/refresh", StringComparison.OrdinalIgnoreCase)
            || requestPath.EndsWith("/users/register", StringComparison.OrdinalIgnoreCase)
            || requestPath.EndsWith("/users/forgot-password", StringComparison.OrdinalIgnoreCase)
            || requestPath.EndsWith("/users/reset-password", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Resolve JWT ID from claims and fallback to bearer token parsing when claim mapping differs.
        Claim? jwtId = context.User.FindFirst(JwtRegisteredClaimNames.Jti) ??
                       context.User.Claims.FirstOrDefault(claim =>
                           claim.Type.EndsWith("/jti", StringComparison.OrdinalIgnoreCase));
        string? jwtIdValue = jwtId?.Value ?? GetJwtIdFromAuthorizationHeader(context.Request.Headers.Authorization);
        if (string.IsNullOrWhiteSpace(jwtIdValue))
        {
            await _next(context);
            return;
        }

        // Check if current JWT token of user is in revocation list
        RevocatedTokenType? revocationType = await _cacheService.GetAsync<RevocatedTokenType?>(jwtIdValue);
        if (revocationType.HasValue)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        string? identityUserId = context.User.FindFirst(CustomClaims.IdentityUserId)?.Value;
        string? securityStamp = context.User.FindFirst(CustomClaims.SecurityStamp)?.Value;
        if (!string.IsNullOrWhiteSpace(identityUserId)
            && !string.IsNullOrWhiteSpace(securityStamp)
            && !await securityStampValidator.IsSecurityStampValidAsync(identityUserId, securityStamp, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static string? GetJwtIdFromAuthorizationHeader(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string token = authorizationHeader[bearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            JwtSecurityToken jwtSecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return string.IsNullOrWhiteSpace(jwtSecurityToken.Id) ? null : jwtSecurityToken.Id;
        }
        catch
        {
            return null;
        }
    }
}
