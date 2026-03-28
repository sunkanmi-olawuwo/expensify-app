using Expensify.Common.Domain;

namespace Expensify.Modules.Users.Application.Abstractions.Preferences;

public interface IUserPreferenceCatalogService
{
    Task<Result<UserPreferenceDefaults>> GetDefaultPreferencesAsync(CancellationToken cancellationToken = default);

    Task<Result> ValidateSelectionsAsync(
        string currency,
        string timezone,
        string? currentCurrency = null,
        string? currentTimezone = null,
        CancellationToken cancellationToken = default);
}

public sealed record UserPreferenceDefaults(string Currency, string Timezone);
