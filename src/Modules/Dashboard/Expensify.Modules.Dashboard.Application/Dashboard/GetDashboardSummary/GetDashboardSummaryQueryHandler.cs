using System.Data.Common;
using System.Globalization;
using Dapper;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;

namespace Expensify.Modules.Dashboard.Application.Dashboard.GetDashboardSummary;

internal sealed class GetDashboardSummaryQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    private static readonly string[] ColorPalette = ["chart-1", "chart-2", "chart-3", "chart-4", "chart-5", "chart-6"];

    public async Task<Result<DashboardSummaryResponse>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string userSettingsSql =
            """
            SELECT
                currency AS Currency,
                timezone AS Timezone,
                month_start_day AS MonthStartDay
            FROM users.users
            WHERE id = @UserId
            """;

        UserSettingsRow? userSettings = await connection.QuerySingleOrDefaultAsync<UserSettingsRow>(
            new CommandDefinition(userSettingsSql, new { request.UserId }, cancellationToken: cancellationToken));

        if (userSettings is null)
        {
            return Result.Failure<DashboardSummaryResponse>(
                Error.NotFound("Dashboard.UserNotFound", $"The user with the identifier {request.UserId} was not found."));
        }

        var currentPeriod = DashboardPeriod.CreateCurrent(dateTimeProvider.UtcNow, userSettings.Timezone, userSettings.MonthStartDay);
        DashboardPeriod previousPeriod = currentPeriod.Previous();
        List<DashboardPeriod> historyPeriods = DashboardPeriod.CreateHistory(currentPeriod, 6);

        const string expensesSql =
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

        const string incomeSql =
            """
            SELECT
                i.amount AS Amount,
                i.income_date AS TransactionDate
            FROM income.incomes i
            WHERE i.user_id = @UserId
              AND i.deleted_at_utc IS NULL
              AND i.income_date >= @WindowStartDate
              AND i.income_date < @WindowEndDateExclusive
            """;

        const string recentTransactionsSql =
            """
            SELECT
                t.id AS Id,
                t.merchant AS Merchant,
                t.category AS Category,
                t.amount AS Amount,
                t.type AS Type,
                t.timestamp AS Timestamp
            FROM (
                SELECT
                    e.id,
                    e.merchant,
                    c.name AS category,
                    e.amount,
                    'expense' AS type,
                    e.created_at_utc AS timestamp
                FROM expenses.expenses e
                INNER JOIN expenses.expense_categories c ON c.id = e.category_id
                WHERE e.user_id = @UserId
                  AND e.deleted_at_utc IS NULL
                  AND e.created_at_utc >= @RecentCutoff

                UNION ALL

                SELECT
                    i.id,
                    i.source AS merchant,
                    i.type AS category,
                    i.amount,
                    'income' AS type,
                    i.created_at_utc AS timestamp
                FROM income.incomes i
                WHERE i.user_id = @UserId
                  AND i.deleted_at_utc IS NULL
                  AND i.created_at_utc >= @RecentCutoff
            ) t
            ORDER BY t.timestamp DESC, t.id DESC
            LIMIT 5
            """;

        var windowStartDate = historyPeriods[0].StartDate.ToDateTime(TimeOnly.MinValue);
        var windowEndDateExclusive = currentPeriod.EndDateExclusive.ToDateTime(TimeOnly.MinValue);
        DateTime recentCutoff = windowStartDate;

        var windowParams = new { request.UserId, WindowStartDate = windowStartDate, WindowEndDateExclusive = windowEndDateExclusive };

        List<ExpenseWindowRow> expenses = (await connection.QueryAsync<ExpenseWindowRow>(
            new CommandDefinition(expensesSql, windowParams, cancellationToken: cancellationToken))).AsList();

        List<IncomeWindowRow> incomes = (await connection.QueryAsync<IncomeWindowRow>(
            new CommandDefinition(incomeSql, windowParams, cancellationToken: cancellationToken))).AsList();

        List<RecentTransactionRow> recentTransactionRows = (await connection.QueryAsync<RecentTransactionRow>(
            new CommandDefinition(recentTransactionsSql, new { request.UserId, RecentCutoff = recentCutoff }, cancellationToken: cancellationToken))).AsList();

        decimal currentIncomeTotal = SumInPeriod(incomes, currentPeriod);
        decimal previousIncomeTotal = SumInPeriod(incomes, previousPeriod);
        decimal currentExpenseTotal = SumInPeriod(expenses, currentPeriod);
        decimal previousExpenseTotal = SumInPeriod(expenses, previousPeriod);
        decimal currentNetCashFlow = currentIncomeTotal - currentExpenseTotal;
        decimal previousNetCashFlow = previousIncomeTotal - previousExpenseTotal;

        DashboardMetricResponse monthlyIncome = new(
            currentIncomeTotal,
            userSettings.Currency,
            CalculateChangePercentage(currentIncomeTotal, previousIncomeTotal));

        DashboardMetricResponse monthlyExpenses = new(
            currentExpenseTotal,
            userSettings.Currency,
            CalculateChangePercentage(currentExpenseTotal, previousExpenseTotal));

        DashboardMetricResponse netCashFlow = new(
            currentNetCashFlow,
            userSettings.Currency,
            CalculateChangePercentage(currentNetCashFlow, previousNetCashFlow));

        List<DashboardSpendingBreakdownItemResponse> spendingBreakdown =
            BuildSpendingBreakdown(expenses, currentPeriod);

        IReadOnlyCollection<DashboardMonthlyPerformanceItemResponse> monthlyPerformance = historyPeriods
            .Select(period => new DashboardMonthlyPerformanceItemResponse(
                period.DisplayLabel,
                SumInPeriod(incomes, period),
                SumInPeriod(expenses, period)))
            .ToList();

        IReadOnlyCollection<DashboardRecentTransactionResponse> recentTransactions = recentTransactionRows
            .Select(row => new DashboardRecentTransactionResponse(
                row.Id,
                row.Merchant,
                row.Category,
                row.Amount,
                row.Type,
                "posted",
                new DateTimeOffset(DateTime.SpecifyKind(row.Timestamp, DateTimeKind.Utc), TimeSpan.Zero)))
            .ToList();

        return new DashboardSummaryResponse(
            monthlyIncome,
            monthlyExpenses,
            netCashFlow,
            spendingBreakdown,
            monthlyPerformance,
            recentTransactions);
    }

    private static decimal SumInPeriod(IEnumerable<IncomeWindowRow> rows, DashboardPeriod period) =>
        rows.Where(row => period.Contains(row.TransactionDate)).Sum(row => row.Amount);

    private static decimal SumInPeriod(IEnumerable<ExpenseWindowRow> rows, DashboardPeriod period) =>
        rows.Where(row => period.Contains(row.TransactionDate)).Sum(row => row.Amount);

    internal static decimal CalculateChangePercentage(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return 0m;
        }

        return Math.Round((current - previous) / Math.Abs(previous) * 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static List<DashboardSpendingBreakdownItemResponse> BuildSpendingBreakdown(
        IEnumerable<ExpenseWindowRow> expenses,
        DashboardPeriod currentPeriod)
    {
        var categories = expenses
            .Where(row => currentPeriod.Contains(row.TransactionDate))
            .GroupBy(row => row.Category, StringComparer.Ordinal)
            .Select(group => new CategoryAmount(group.Key, group.Sum(item => item.Amount)))
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.Category, StringComparer.Ordinal)
            .ToList();

        if (categories.Count == 0)
        {
            return [];
        }

        decimal total = categories.Sum(item => item.Amount);
        var breakdown = new List<DashboardSpendingBreakdownItemResponse>(categories.Count);
        decimal runningPercentage = 0m;

        for (int i = 0; i < categories.Count; i++)
        {
            CategoryAmount category = categories[i];
            decimal percentage = i == categories.Count - 1
                ? Math.Round(100m - runningPercentage, 2, MidpointRounding.AwayFromZero)
                : Math.Round(category.Amount / total * 100m, 2, MidpointRounding.AwayFromZero);

            runningPercentage += percentage;

            breakdown.Add(new DashboardSpendingBreakdownItemResponse(
                category.Category,
                category.Amount,
                percentage,
                ColorPalette[i % ColorPalette.Length]));
        }

        return breakdown;
    }

    private sealed record CategoryAmount(string Category, decimal Amount);

    internal sealed class UserSettingsRow
    {
        public string Currency { get; init; } = string.Empty;
        public string Timezone { get; init; } = "UTC";
        public int MonthStartDay { get; init; } = 1;
    }

    internal sealed class ExpenseWindowRow
    {
        public decimal Amount { get; init; } = 0m;
        public DateOnly TransactionDate { get; init; } = new(2000, 1, 1);
        public string Category { get; init; } = string.Empty;
    }

    internal sealed class IncomeWindowRow
    {
        public decimal Amount { get; init; } = 0m;
        public DateOnly TransactionDate { get; init; } = new(2000, 1, 1);
    }

    internal sealed class RecentTransactionRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public string Merchant { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public decimal Amount { get; init; } = 0m;
        public string Type { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; } = DateTime.UnixEpoch;
    }

    internal sealed record DashboardPeriod(DateOnly StartDate, DateOnly EndDateExclusive, string DisplayLabel)
    {
        public static DashboardPeriod CreateCurrent(DateTime utcNow, string timezoneId, int monthStartDay)
        {
            TimeZoneInfo timeZone = ResolveTimeZone(timezoneId);
            DateTime normalizedUtcNow = utcNow.Kind == DateTimeKind.Utc
                ? utcNow
                : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(normalizedUtcNow, timeZone));
            int safeMonthStartDay = Math.Clamp(monthStartDay, 1, 28);
            DateOnly startMonth = new(localDate.Year, localDate.Month, 1);

            if (localDate.Day < safeMonthStartDay)
            {
                startMonth = startMonth.AddMonths(-1);
            }

            DateOnly startDate = new(startMonth.Year, startMonth.Month, safeMonthStartDay);
            return Create(startDate);
        }

        public static List<DashboardPeriod> CreateHistory(DashboardPeriod currentPeriod, int count)
        {
            return Enumerable.Range(0, count)
                .Select(offset => Create(currentPeriod.StartDate.AddMonths(offset - (count - 1))))
                .ToList();
        }

        public DashboardPeriod Previous() => Create(StartDate.AddMonths(-1));

        public bool Contains(DateOnly date) => date >= StartDate && date < EndDateExclusive;

        private static DashboardPeriod Create(DateOnly startDate)
        {
            return new DashboardPeriod(
                startDate,
                startDate.AddMonths(1),
                startDate.ToString("MMM yyyy", CultureInfo.InvariantCulture));
        }

        private static TimeZoneInfo ResolveTimeZone(string timezoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
}
