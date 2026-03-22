namespace Expensify.Modules.Users.Application.Abstractions.Identity;

public interface IPasswordResetNotifier
{
    Task SendPasswordResetLinkAsync(string email, string encodedToken, CancellationToken cancellationToken = default);
}
