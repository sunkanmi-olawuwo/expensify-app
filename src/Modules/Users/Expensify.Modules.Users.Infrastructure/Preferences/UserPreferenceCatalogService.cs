using Microsoft.EntityFrameworkCore;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions.Preferences;
using Expensify.Modules.Users.Domain.Preferences;
using Expensify.Modules.Users.Infrastructure.Database;

namespace Expensify.Modules.Users.Infrastructure.Preferences;

internal sealed class UserPreferenceCatalogService(UsersDbContext dbContext) : IUserPreferenceCatalogService
{
    public async Task<Result<UserPreferenceDefaults>> GetDefaultPreferencesAsync(CancellationToken cancellationToken = default)
    {
        string? defaultCurrency = await dbContext.Currencies
            .Where(currency => currency.IsActive && currency.IsDefault)
            .Select(currency => currency.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (defaultCurrency is null)
        {
            return Result.Failure<UserPreferenceDefaults>(PreferenceCatalogErrors.DefaultCurrencyRequired());
        }

        string? defaultTimezone = await dbContext.Timezones
            .Where(timezone => timezone.IsActive && timezone.IsDefault)
            .Select(timezone => timezone.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (defaultTimezone is null)
        {
            return Result.Failure<UserPreferenceDefaults>(PreferenceCatalogErrors.DefaultTimezoneRequired());
        }

        return new UserPreferenceDefaults(defaultCurrency, defaultTimezone);
    }

    public async Task<Result> ValidateSelectionsAsync(
        string currency,
        string timezone,
        string? currentCurrency = null,
        string? currentTimezone = null,
        CancellationToken cancellationToken = default)
    {
        Result currencyValidationResult = await ValidateCurrencyAsync(currency, currentCurrency, cancellationToken);
        if (currencyValidationResult.IsFailure)
        {
            return currencyValidationResult;
        }

        return await ValidateTimezoneAsync(timezone, currentTimezone, cancellationToken);
    }

    private async Task<Result> ValidateCurrencyAsync(string currency, string? currentCurrency, CancellationToken cancellationToken)
    {
        bool? isActive = await dbContext.Currencies
            .Where(item => item.Id == currency)
            .Select(item => (bool?)item.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (isActive is null)
        {
            return Result.Failure(PreferenceCatalogErrors.CurrencyNotAllowed(currency));
        }

        if (isActive.Value || string.Equals(currency, currentCurrency, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        return Result.Failure(PreferenceCatalogErrors.CurrencyNotAllowed(currency));
    }

    private async Task<Result> ValidateTimezoneAsync(string timezone, string? currentTimezone, CancellationToken cancellationToken)
    {
        bool? isActive = await dbContext.Timezones
            .Where(item => item.Id == timezone)
            .Select(item => (bool?)item.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (isActive is null)
        {
            return Result.Failure(PreferenceCatalogErrors.TimezoneNotAllowed(timezone));
        }

        if (isActive.Value || string.Equals(timezone, currentTimezone, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        return Result.Failure(PreferenceCatalogErrors.TimezoneNotAllowed(timezone));
    }
}
