using Expensify.Common.Domain;

namespace Expensify.Modules.Users.Domain.Preferences;

public static class PreferenceCatalogErrors
{
    public static Error CurrencyAlreadyExists(string code) =>
        Error.Conflict("Users.CurrencyAlreadyExists", $"Currency '{code}' already exists.");

    public static Error CurrencyNotFound(string code) =>
        Error.NotFound("Users.CurrencyNotFound", $"Currency '{code}' was not found.");

    public static Error TimezoneAlreadyExists(string ianaId) =>
        Error.Conflict("Users.TimezoneAlreadyExists", $"Timezone '{ianaId}' already exists.");

    public static Error TimezoneNotFound(string ianaId) =>
        Error.NotFound("Users.TimezoneNotFound", $"Timezone '{ianaId}' was not found.");

    public static Error CurrencyMustRemainActiveWhenDefault() =>
        Error.Validation("Users.CurrencyDefaultMustBeActive", "A default currency must remain active.");

    public static Error TimezoneMustRemainActiveWhenDefault() =>
        Error.Validation("Users.TimezoneDefaultMustBeActive", "A default timezone must remain active.");

    public static Error DefaultCurrencyRequired() =>
        Error.Validation("Users.DefaultCurrencyRequired", "At least one active default currency is required.");

    public static Error DefaultTimezoneRequired() =>
        Error.Validation("Users.DefaultTimezoneRequired", "At least one active default timezone is required.");

    public static Error CurrencyNotAllowed(string code) =>
        Error.Validation("Users.InvalidCurrency", $"Currency '{code}' is not an active allowed currency.");

    public static Error TimezoneNotAllowed(string ianaId) =>
        Error.Validation("Users.InvalidTimezone", $"Timezone '{ianaId}' is not an active allowed timezone.");

    public static Error DefaultCurrencyConflict() =>
        Error.Conflict("Users.DefaultCurrencyConflict", "A concurrent request modified the default currency. Please retry.");

    public static Error DefaultTimezoneConflict() =>
        Error.Conflict("Users.DefaultTimezoneConflict", "A concurrent request modified the default timezone. Please retry.");
}
