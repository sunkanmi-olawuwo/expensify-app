using Expensify.Modules.Users.Application.Abstractions.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Expensify.IntegrationTests.StepDefinitions.Users;

public sealed class InMemoryPasswordResetNotifier : IPasswordResetNotifier
{
    private const string ResetUrlBase = "https://integration.expensify.test/reset-password";
    private readonly object _lock = new();
    private readonly List<PasswordResetDelivery> _deliveries = [];

    public Task SendPasswordResetLinkAsync(string email, string encodedToken, CancellationToken cancellationToken = default)
    {
        Uri resetUrl = new(QueryHelpers.AddQueryString(
            ResetUrlBase,
            new Dictionary<string, string?>
            {
                ["email"] = email,
                ["token"] = encodedToken
            }),
            UriKind.Absolute);

        lock (_lock)
        {
            _deliveries.Add(new PasswordResetDelivery(email, encodedToken, resetUrl));
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _deliveries.Clear();
        }
    }

    public PasswordResetDelivery? GetLatest(string email)
    {
        lock (_lock)
        {
            return _deliveries.LastOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed record PasswordResetDelivery(string Email, string EncodedToken, Uri ResetUrl);
}
