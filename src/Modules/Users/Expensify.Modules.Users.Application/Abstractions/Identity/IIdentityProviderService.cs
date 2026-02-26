using Expensify.Common.Domain;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Application.Abstractions.Identity;

public interface IIdentityProviderService
{
    Task<Result<string>> RegisterUserAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

    Task<Result<LoginUserResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<Result<RefreshTokenResponse>> RefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken);

    Task<Result> UpdateUserRoleAsync(Guid domainUserId, string identityUserId, RoleType newRole, CancellationToken cancellationToken);

    Task<Result> DeleteUserAsync(string identityId, CancellationToken cancellationToken = default);
}
    
