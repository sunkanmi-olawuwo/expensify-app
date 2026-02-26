using JetBrains.Annotations;

namespace Expensify.Common.Application;

[UsedImplicitly]
public static class ApplicationConstants
{
    public const string CorsPolicy = nameof(CorsPolicy);
    public const string ApplicationName = "Expensify.API";
    public const string Schema = "Expensify_db";
    public const string MigrationTableName = "migration_history";
    public const string AdminRole = "Admin";
    public const string UserRole = "User";
}
