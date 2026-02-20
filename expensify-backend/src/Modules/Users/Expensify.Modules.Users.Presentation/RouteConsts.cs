namespace Expensify.Modules.Users.Presentation;

internal static class RouteConsts
{
    private const string BaseRoute = "/users";

    internal const string UserProfile = $"{BaseRoute}/{{Id}}/profile";

    internal const string Login = $"{BaseRoute}/login";

    internal const string Register = $"{BaseRoute}/register";

    internal const string RefreshToken = $"{BaseRoute}/refresh";

    internal const string UpdateUser = $"{BaseRoute}/{{id}}/profile";

    internal const string DeleteUser = $"{BaseRoute}/{{Id}}";

    internal const string UpdateUserRole = $"{BaseRoute}/{{id}}/role";

    internal const string GetUsers = BaseRoute;
}
