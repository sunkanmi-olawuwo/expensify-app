using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Expensify.Common.Application.Caching;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Infrastructure.Identity;

internal sealed class UserSessionService(
    UserManager<IdentityUser> userManager,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    ILogger<UserSessionService> logger)
    : IUserSessionService, IIdentitySecurityStampValidator
{
    private const string SecurityStampCacheKeyPrefix = "users:security-stamp:";
    private static readonly TimeSpan SecurityStampCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan RevokedJwtCacheDuration = TimeSpan.FromMinutes(30);

    public Task CacheSecurityStampAsync(string identityUserId, string securityStamp, CancellationToken cancellationToken = default)
    {
        return cacheService.SetAsync(
            GetSecurityStampCacheKey(identityUserId),
            securityStamp,
            SecurityStampCacheDuration,
            cancellationToken);
    }

    public async Task<bool> IsSecurityStampValidAsync(string identityUserId, string securityStamp, CancellationToken cancellationToken = default)
    {
        string cacheKey = GetSecurityStampCacheKey(identityUserId);
        string? currentSecurityStamp = await cacheService.GetAsync<string>(cacheKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(currentSecurityStamp))
        {
            IdentityUser? identityUser = await userManager.FindByIdAsync(identityUserId);
            if (identityUser is null)
            {
                logger.LogWarning("Identity user with ID '{IdentityId}' not found while validating security stamp", identityUserId);
                return false;
            }

            currentSecurityStamp = await userManager.GetSecurityStampAsync(identityUser);
            await CacheSecurityStampAsync(identityUserId, currentSecurityStamp, cancellationToken);
        }

        return string.Equals(currentSecurityStamp, securityStamp, StringComparison.Ordinal);
    }

    public async Task<Result> InvalidateAllSessionsAsync(
        Guid domainUserId,
        string identityUserId,
        RevocatedTokenType revocationType,
        CancellationToken cancellationToken = default)
    {
        List<RefreshToken> refreshTokens = await refreshTokenRepository.GetValidTokenAsync(domainUserId, cancellationToken);

        foreach (RefreshToken refreshToken in refreshTokens)
        {
            if (!refreshToken.Invalidated)
            {
                RefreshToken.Invalidate(refreshToken);
            }

            await cacheService.SetAsync(
                refreshToken.JwtId,
                revocationType,
                RevokedJwtCacheDuration,
                cancellationToken);
        }

        IdentityUser? identityUser = await userManager.FindByIdAsync(identityUserId);
        if (identityUser is null)
        {
            logger.LogWarning("Identity user with ID '{IdentityId}' not found while invalidating sessions", identityUserId);
            return Result.Failure(UserErrors.NotFound(identityUserId));
        }

        IdentityResult updateSecurityStampResult = await userManager.UpdateSecurityStampAsync(identityUser);
        if (!updateSecurityStampResult.Succeeded)
        {
            logger.LogError(
                "Failed to update security stamp for user '{IdentityId}': {@Errors}",
                identityUser.Id,
                updateSecurityStampResult.Errors);
            return Result.Failure(UserErrors.SessionInvalidationFailed(updateSecurityStampResult.Errors));
        }

        string updatedSecurityStamp = await userManager.GetSecurityStampAsync(identityUser);
        await CacheSecurityStampAsync(identityUser.Id, updatedSecurityStamp, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string GetSecurityStampCacheKey(string identityUserId) =>
        $"{SecurityStampCacheKeyPrefix}{identityUserId}";
}
