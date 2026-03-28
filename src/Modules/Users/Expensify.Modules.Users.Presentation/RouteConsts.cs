namespace Expensify.Modules.Users.Presentation;

internal static class RouteConsts
{
    private const string BaseRoute = "/users";
    private const string CurrenciesBase = "/currencies";
    private const string TimezonesBase = "/timezones";

    internal const string UserProfile = $"{BaseRoute}/profile";

    internal const string Login = $"{BaseRoute}/login";

    internal const string Register = $"{BaseRoute}/register";

    internal const string RefreshToken = $"{BaseRoute}/refresh";

    internal const string Logout = $"{BaseRoute}/logout";

    internal const string ChangePassword = $"{BaseRoute}/change-password";

    internal const string ForgotPassword = $"{BaseRoute}/forgot-password";

    internal const string ResetPassword = $"{BaseRoute}/reset-password";

    internal const string UpdateUser = $"{BaseRoute}/profile";

    internal const string DeleteUser = $"{BaseRoute}/{{Id}}";

    internal const string UpdateUserRole = $"{BaseRoute}/{{id}}/role";

    internal const string GetUsers = BaseRoute;

    internal const string Currencies = CurrenciesBase;

    internal const string CurrencyByCode = $"{CurrenciesBase}/{{code}}";

    internal const string Timezones = TimezonesBase;

    internal const string TimezoneById = $"{TimezonesBase}/{{ianaId}}";
}
