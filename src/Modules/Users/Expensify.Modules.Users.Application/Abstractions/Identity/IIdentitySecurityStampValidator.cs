namespace Expensify.Modules.Users.Application.Abstractions.Identity;

public interface IIdentitySecurityStampValidator
{
    Task<bool> IsSecurityStampValidAsync(string identityUserId, string securityStamp, CancellationToken cancellationToken = default);
}
