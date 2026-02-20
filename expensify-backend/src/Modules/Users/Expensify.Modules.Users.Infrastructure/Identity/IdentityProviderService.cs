using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Expensify.Common.Application.Caching;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Common.Infrastructure.Authentication;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Abstractions.Identity;
using Expensify.Modules.Users.Domain.Identity;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Infrastructure.Identity;

internal sealed class IdentityProviderService(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    RoleManager<Role> roleManager,
    IOptions<AuthSettings> authOptions,
    TokenValidationParameters tokenValidationParameters,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    ILogger<IdentityProviderService> logger)
    : IIdentityProviderService
{
    private const string RoleClaimsCacheKeyPrefix = "users:roles:claims:";
    private static readonly TimeSpan RoleClaimsCacheDuration = TimeSpan.FromMinutes(10);

    public async Task<Result<LoginUserResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        IdentityUser identityUser = await userManager.FindByEmailAsync(email);

        if (identityUser == null)
        {
            return Result.Failure<LoginUserResponse>(UserErrors.NotFoundByEmail(email));
        }

        SignInResult result = await signInManager.CheckPasswordSignInAsync(identityUser, password, false);
        if (!result.Succeeded)
        {
            return Result.Failure<LoginUserResponse>(UserErrors.InvalidCredentials());
        }

        (string? token, string? refreshToken) = await GenerateJwtAndRefreshTokenAsync(identityUser, null, cancellationToken);

        return Result.Success(new LoginUserResponse(token, refreshToken));
    }

    public async Task<Result<string>> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        IdentityUser identityUser = new()
        {
            UserName = request.Email,
            Email = request.Email,
            NormalizedEmail = request.Email.ToUpperInvariant(),
            NormalizedUserName = request.Email.ToUpperInvariant(),
        };
        IdentityResult result = await userManager.CreateAsync(identityUser, request.Password);

        if (!result.Succeeded)
        {
            logger.LogError("Failed to register user: {@Errors}", result.Errors);
            return Result.Failure<string>(UserErrors.RegistrationFailed(result.Errors));
        }


        await userManager.AddToRoleAsync(identityUser, request.Role.ToString());


        logger.LogInformation("Created user with Identity ID: {IdentityId}", identityUser.Id);

        return Result.Success(identityUser.Id);
    }

    private async Task<(string token, string refreshToken)> GenerateJwtAndRefreshTokenAsync(
        IdentityUser identityUser,
        string? existingRefreshToken,
        CancellationToken cancellationToken)
    {
        IList<string> roles = await userManager.GetRolesAsync(identityUser);
        string userRole = roles.FirstOrDefault() ?? "user";

        IReadOnlyCollection<Claim> roleClaims = await GetRoleClaimsAsync(userRole, cancellationToken);

        User? domainUser = await userRepository.GetByIdentityIdAsync(identityUser.Id, cancellationToken);
        if (domainUser == null)
        {
            logger.LogWarning("User with Identity ID '{IdentityId}' not found", identityUser.Id);
            return (string.Empty, string.Empty);
        }
        string token = await GenerateJwtTokenAsync(domainUser.Id, identityUser, authOptions.Value, userRole, roleClaims);
        string refreshToken = await GenerateRefreshTokenAsync(domainUser.Id, token, existingRefreshToken, cancellationToken);

        return (token, refreshToken);
    }

    private static async Task<string> GenerateJwtTokenAsync(Guid domainUserId, IdentityUser identityUser,
        AuthSettings authConfiguration,
        string userRole,
        IReadOnlyCollection<Claim> roleClaims)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authConfiguration.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        string tokenId = Guid.NewGuid().ToString();
        List<Claim> claims = [
            new(JwtRegisteredClaimNames.Sub, identityUser.Email!),
            new("userid", domainUserId.ToString()),
            new("role", userRole),
            new(JwtRegisteredClaimNames.Jti, tokenId)
        ];

        foreach (Claim roleClaim in roleClaims)
        {
            claims.Add(new Claim(roleClaim.Type, roleClaim.Value));
        }

        var token = new JwtSecurityToken(
            issuer: authConfiguration.Issuer,
            audience: authConfiguration.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(
        Guid domainUserId,
        string token,
        string? existingRefreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);
        string jti = jwtToken.Id;

        var refreshToken = RefreshToken.Create(
            token: Guid.NewGuid().ToString(),
            jwtId: jti,
            userId: domainUserId
        );

        if (!string.IsNullOrEmpty(existingRefreshToken))
        {
            RefreshToken? existingToken = await refreshTokenRepository.GetByIdAsync(existingRefreshToken, cancellationToken);
            if (existingToken != null)
            {
                refreshTokenRepository.Remove(existingToken);
            }
        }

        refreshTokenRepository.Add(refreshToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return refreshToken.Id;
    }

    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken)
    {
        ClaimsPrincipal? validatedToken = GetPrincipalFromToken(token, tokenValidationParameters);
        if (validatedToken is null)
        {
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        string? jti = validatedToken.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
        {
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        RefreshToken? storedRefreshToken = await refreshTokenRepository.GetByIdAsync(refreshToken, cancellationToken);
        if (storedRefreshToken is null)
        {
            logger.LogWarning("Refresh token does not exist");
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
        {
            logger.LogWarning("Refresh token has expired");
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        if (storedRefreshToken.Invalidated)
        {
            logger.LogWarning("Refresh token has been invalidated");
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        if (storedRefreshToken.JwtId != jti)
        {
            logger.LogWarning("Refresh token does not match this JWT");
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        string? userIdString = validatedToken.Claims.FirstOrDefault(x => x.Type == "userid")?.Value;
        if (userIdString is null || !Guid.TryParse(userIdString, out Guid userId))
        {
            logger.LogWarning("User ID claim not found or invalid");
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        User? domainUser = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (domainUser is null)
        {
            logger.LogWarning("Domain user with ID '{UserId}' not found", userId);
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        IdentityUser? identityUser = await userManager.FindByIdAsync(domainUser.IdentityId);
        if (identityUser is null)
        {
            logger.LogWarning("Identity user with ID '{IdentityId}' not found", domainUser.IdentityId);
            return Result.Failure<RefreshTokenResponse>(UserErrors.InvalidToken());
        }

        (string? newToken, string? newRefreshToken) = await GenerateJwtAndRefreshTokenAsync(identityUser, refreshToken, cancellationToken);
        return Result.Success(new RefreshTokenResponse(newToken, newRefreshToken));
    }

    private static ClaimsPrincipal? GetPrincipalFromToken(string token, TokenValidationParameters parameters)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            TokenValidationParameters tokenValidationParameters = parameters.Clone();

#pragma warning disable CA5404
            tokenValidationParameters.ValidateLifetime = false;
#pragma warning restore CA5404

            ClaimsPrincipal principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken? validatedToken);
            return IsJwtWithValidSecurityAlgorithm(validatedToken) ? principal : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
       => validatedToken is JwtSecurityToken jwtSecurityToken
          && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase);

    public async Task<Result> UpdateUserRoleAsync(Guid domainUserId, string identityUserId, RoleType newRole, CancellationToken cancellationToken)
    {
        // Verify the role exists
        Role? role = await roleManager.FindByNameAsync(newRole.ToString());
        if (role is null)
        {
            logger.LogWarning("Role '{NewRole}' does not exist", newRole);
            return Result.Failure(UserErrors.RoleNotFound(newRole.ToString()));
        }

        // Find the user
        IdentityUser? user = await userManager.FindByIdAsync(identityUserId);
        if (user is null)
        {
            logger.LogWarning("Identity user with ID '{IdentityId}' not found", identityUserId);
            return Result.Failure(UserErrors.NotFound(identityUserId));
        }

        // Get current roles and remove them
        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        // Add the new role
        IdentityResult addRoleResult = await userManager.AddToRoleAsync(user, newRole.ToString());
        if (!addRoleResult.Succeeded)
        {
            logger.LogError("Failed to add role '{NewRole}' to user '{UserId}': {@Errors}", newRole, identityUserId, addRoleResult.Errors);
            return Result.Failure(UserErrors.UpdateRoleFailed(addRoleResult.Errors));
        }

        // Force next token generation to read claims from role manager after role updates.
        await InvalidateRoleClaimsCacheAsync(currentRoles.Append(newRole.ToString()), cancellationToken);

        // Invalidate all refresh tokens for this user
        List<RefreshToken> refreshTokens = await refreshTokenRepository.GetValidTokenAsync(domainUserId, cancellationToken);

        foreach (RefreshToken refreshToken in refreshTokens)
        {
            refreshToken.Invalidated = true;
            refreshToken.UpdatedAtUtc = DateTime.UtcNow;

            // Add to cache for the middleware to check
            await cacheService.SetAsync(refreshToken.JwtId, RevocatedTokenType.RoleChanged, cancellationToken: cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(string identityId, CancellationToken cancellationToken = default)
    {
        IdentityUser? user = await userManager.FindByIdAsync(identityId);
        if (user is null)
        {
            logger.LogWarning("Identity user with ID '{IdentityId}' not found", identityId);
            return Result.Failure(UserErrors.NotFound(identityId));
        }
        IdentityResult deleteUserResult = await userManager.DeleteAsync(user);
        if (!deleteUserResult.Succeeded)
        {
            logger.LogError("Failed to delete user '{UserId}': {@Errors}", identityId, deleteUserResult.Errors);
            return Result.Failure(UserErrors.DeleteUserFailed(deleteUserResult.Errors));
        }

        return Result.Success();
    }

    private async Task<IReadOnlyCollection<Claim>> GetRoleClaimsAsync(string roleName, CancellationToken cancellationToken)
    {
        string cacheKey = GetRoleClaimsCacheKey(roleName);
        List<RoleClaimCacheItem>? cachedRoleClaims = await cacheService.GetAsync<List<RoleClaimCacheItem>>(cacheKey, cancellationToken);
        if (cachedRoleClaims is not null)
        {
            return cachedRoleClaims
                .Select(claim => new Claim(claim.Type, claim.Value))
                .ToList();
        }

        Role? role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            return [];
        }

        IList<Claim> roleClaims = await roleManager.GetClaimsAsync(role);
        var roleClaimItems = roleClaims
            .Select(claim => new RoleClaimCacheItem(claim.Type, claim.Value))
            .ToList();

        await cacheService.SetAsync(cacheKey, roleClaimItems, RoleClaimsCacheDuration, cancellationToken);

        return roleClaims.ToList();
    }

    private async Task InvalidateRoleClaimsCacheAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        foreach (string roleName in roleNames
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await cacheService.RemoveAsync(GetRoleClaimsCacheKey(roleName), cancellationToken);
        }
    }

    private static string GetRoleClaimsCacheKey(string roleName) =>
        $"{RoleClaimsCacheKeyPrefix}{roleName.ToLowerInvariant()}";

    private sealed record RoleClaimCacheItem(string Type, string Value);
}
