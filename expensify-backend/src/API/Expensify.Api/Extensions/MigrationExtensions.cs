using Microsoft.EntityFrameworkCore;
using Expensify.Modules.Expenses.Infrastructure.Database;
using Expensify.Modules.Income.Infrastructure.Database;
using Expensify.Modules.Users.Infrastructure.Database;

namespace Expensify.Api.Extensions;

internal static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        ApplyMigration<UsersDbContext>(scope);
        ApplyMigration<ExpensesDbContext>(scope);
        ApplyMigration<IncomeDbContext>(scope);
    }

    private static void ApplyMigration<TDbContext>(IServiceScope scope)
        where TDbContext : DbContext
    {
        using TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        context.Database.Migrate();
    }
}
