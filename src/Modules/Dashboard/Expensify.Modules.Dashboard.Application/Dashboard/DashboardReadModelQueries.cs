using System.Data.Common;
using Dapper;
using Expensify.Common.Domain;

namespace Expensify.Modules.Dashboard.Application.Dashboard;

internal static class DashboardReadModelQueries
{
    public static async Task<Result<DashboardUserSettings>> GetUserSettingsAsync(
        DbConnection connection,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                currency AS Currency,
                timezone AS Timezone,
                month_start_day AS MonthStartDay
            FROM users.users
            WHERE id = @UserId
            """;

        DashboardUserSettings? userSettings = await connection.QuerySingleOrDefaultAsync<DashboardUserSettings>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        if (userSettings is null)
        {
            return Result.Failure<DashboardUserSettings>(
                Error.NotFound("Dashboard.UserNotFound", $"The user with the identifier {userId} was not found."));
        }

        return userSettings;
    }

    public static async Task<List<DashboardExpenseRow>> GetExpensesAsync(
        DbConnection connection,
        Guid userId,
        DateOnly windowStartDate,
        DateOnly windowEndDateExclusive,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                e.amount AS Amount,
                e.expense_date AS TransactionDate,
                c.name AS Category
            FROM expenses.expenses e
            INNER JOIN expenses.expense_categories c ON c.id = e.category_id
            WHERE e.user_id = @UserId
              AND e.deleted_at_utc IS NULL
              AND e.expense_date >= @WindowStartDate
              AND e.expense_date < @WindowEndDateExclusive
            """;

        return (await connection.QueryAsync<DashboardExpenseRow>(
            new CommandDefinition(
                sql,
                new
                {
                    UserId = userId,
                    WindowStartDate = windowStartDate.ToDateTime(TimeOnly.MinValue),
                    WindowEndDateExclusive = windowEndDateExclusive.ToDateTime(TimeOnly.MinValue)
                },
                cancellationToken: cancellationToken))).AsList();
    }

    public static async Task<List<DashboardIncomeRow>> GetIncomesAsync(
        DbConnection connection,
        Guid userId,
        DateOnly windowStartDate,
        DateOnly windowEndDateExclusive,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                i.amount AS Amount,
                i.income_date AS TransactionDate,
                i.type AS Type
            FROM income.incomes i
            WHERE i.user_id = @UserId
              AND i.deleted_at_utc IS NULL
              AND i.income_date >= @WindowStartDate
              AND i.income_date < @WindowEndDateExclusive
            """;

        return (await connection.QueryAsync<DashboardIncomeRow>(
            new CommandDefinition(
                sql,
                new
                {
                    UserId = userId,
                    WindowStartDate = windowStartDate.ToDateTime(TimeOnly.MinValue),
                    WindowEndDateExclusive = windowEndDateExclusive.ToDateTime(TimeOnly.MinValue)
                },
                cancellationToken: cancellationToken))).AsList();
    }

    public static async Task<List<DashboardInvestmentAllocationRow>> GetInvestmentAllocationAsync(
        DbConnection connection,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                c.name AS CategoryName,
                c.slug AS CategorySlug,
                SUM(a.current_balance) AS TotalBalance,
                COUNT(*) AS AccountCount
            FROM investments.investment_accounts a
            INNER JOIN investments.investment_categories c ON c.id = a.category_id
            WHERE a.user_id = @UserId
              AND a.deleted_at_utc IS NULL
            GROUP BY c.name, c.slug
            ORDER BY TotalBalance DESC, c.name
            """;

        return (await connection.QueryAsync<DashboardInvestmentAllocationRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken))).AsList();
    }

    public static async Task<List<DashboardInvestmentContributionRow>> GetInvestmentContributionsAsync(
        DbConnection connection,
        Guid userId,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndDateExclusiveUtc,
        CancellationToken cancellationToken)
    {
        const string sql =
            """
            SELECT
                ic.investment_id AS InvestmentId,
                ic.amount AS Amount,
                ic.date AS ContributionDate
            FROM investments.investment_contributions ic
            INNER JOIN investments.investment_accounts ia ON ia.id = ic.investment_id
            WHERE ia.user_id = @UserId
              AND ia.deleted_at_utc IS NULL
              AND ic.deleted_at_utc IS NULL
              AND ic.date >= @WindowStartUtc
              AND ic.date < @WindowEndDateExclusiveUtc
            """;

        return (await connection.QueryAsync<DashboardInvestmentContributionRow>(
            new CommandDefinition(
                sql,
                new
                {
                    UserId = userId,
                    WindowStartUtc = windowStartUtc,
                    WindowEndDateExclusiveUtc = windowEndDateExclusiveUtc
                },
                cancellationToken: cancellationToken))).AsList();
    }
}

internal sealed class DashboardUserSettings
{
    public string Currency { get; init; } = string.Empty;
    public string Timezone { get; init; } = "UTC";
    public int MonthStartDay { get; init; } = 1;
}

internal sealed class DashboardExpenseRow
{
    public decimal Amount { get; init; } = 0m;
    public DateOnly TransactionDate { get; init; } = new(2000, 1, 1);
    public string Category { get; init; } = string.Empty;
}

internal sealed class DashboardIncomeRow
{
    public decimal Amount { get; init; } = 0m;
    public DateOnly TransactionDate { get; init; } = new(2000, 1, 1);
    public string Type { get; init; } = string.Empty;
}

internal sealed class DashboardInvestmentAllocationRow
{
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;
    public decimal TotalBalance { get; init; } = 0m;
    public int AccountCount { get; init; }
}

internal sealed class DashboardInvestmentContributionRow
{
    public Guid InvestmentId { get; init; } = Guid.Empty;
    public decimal Amount { get; init; } = 0m;
    public DateTimeOffset ContributionDate { get; init; } = DateTimeOffset.UnixEpoch;
}
