using Expensify.Common.Domain;
using Expensify.Modules.Users.Domain.Tokens;

namespace Expensify.Modules.Users.Infrastructure.Identity;

public interface IUserSessionService
{
    Task CacheSecurityStampAsync(string identityUserId, string securityStamp, CancellationToken cancellationToken = default);

    Task<Result> InvalidateAllSessionsAsync(
        Guid domainUserId,
        string identityUserId,
        RevocatedTokenType revocationType,
        CancellationToken cancellationToken = default);
}
