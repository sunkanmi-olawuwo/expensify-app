using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;

namespace Expensify.Modules.Dashboard.Application.Dashboard.GetDashboardSummary;

internal sealed class GetDashboardSummaryQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryResponse>
{
    public async Task<Result<DashboardSummaryResponse>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        Result<DashboardUserSettings> userSettingsResult =
            await DashboardReadModelQueries.GetUserSettingsAsync(connection, request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<DashboardSummaryResponse>(userSettingsResult.Error);
        }

        DashboardUserSettings userSettings = userSettingsResult.Value;
        var currentPeriod = DashboardPeriod.CreateCurrent(dateTimeProvider.UtcNow, userSettings.Timezone, userSettings.MonthStartDay);
        DashboardPeriod previousPeriod = currentPeriod.Previous();
        List<DashboardPeriod> historyPeriods = DashboardPeriod.CreateHistory(currentPeriod, 6);

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

        DateOnly windowStartDate = historyPeriods[0].StartDate;
        DateOnly windowEndDateExclusive = currentPeriod.EndDateExclusive;
        var recentCutoff = windowStartDate.ToDateTime(TimeOnly.MinValue);

        List<DashboardExpenseRow> expenses = await DashboardReadModelQueries.GetExpensesAsync(
            connection,
            request.UserId,
            windowStartDate,
            windowEndDateExclusive,
            cancellationToken);

        List<DashboardIncomeRow> incomes = await DashboardReadModelQueries.GetIncomesAsync(
            connection,
            request.UserId,
            windowStartDate,
            windowEndDateExclusive,
            cancellationToken);

        List<RecentTransactionRow> recentTransactionRows = (await connection.QueryAsync<RecentTransactionRow>(
            new CommandDefinition(recentTransactionsSql, new { request.UserId, RecentCutoff = recentCutoff }, cancellationToken: cancellationToken))).AsList();

        decimal currentIncomeTotal = DashboardCalculations.SumInPeriod(incomes, currentPeriod);
        decimal previousIncomeTotal = DashboardCalculations.SumInPeriod(incomes, previousPeriod);
        decimal currentExpenseTotal = DashboardCalculations.SumInPeriod(expenses, currentPeriod);
        decimal previousExpenseTotal = DashboardCalculations.SumInPeriod(expenses, previousPeriod);
        decimal currentNetCashFlow = currentIncomeTotal - currentExpenseTotal;
        decimal previousNetCashFlow = previousIncomeTotal - previousExpenseTotal;

        DashboardMetricResponse monthlyIncome = new(
            currentIncomeTotal,
            userSettings.Currency,
            DashboardCalculations.CalculateChangePercentage(currentIncomeTotal, previousIncomeTotal));

        DashboardMetricResponse monthlyExpenses = new(
            currentExpenseTotal,
            userSettings.Currency,
            DashboardCalculations.CalculateChangePercentage(currentExpenseTotal, previousExpenseTotal));

        DashboardMetricResponse netCashFlow = new(
            currentNetCashFlow,
            userSettings.Currency,
            DashboardCalculations.CalculateChangePercentage(currentNetCashFlow, previousNetCashFlow));

        List<DashboardSpendingBreakdownItemResponse> spendingBreakdown =
            BuildSpendingBreakdown(expenses, currentPeriod);

        IReadOnlyCollection<DashboardMonthlyPerformanceItemResponse> monthlyPerformance = historyPeriods
            .Select(period => new DashboardMonthlyPerformanceItemResponse(
                period.DisplayLabel,
                DashboardCalculations.SumInPeriod(incomes, period),
                DashboardCalculations.SumInPeriod(expenses, period)))
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

    private static List<DashboardSpendingBreakdownItemResponse> BuildSpendingBreakdown(
        IEnumerable<DashboardExpenseRow> expenses,
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
        IReadOnlyList<decimal> percentages = DashboardCalculations.CalculatePercentages(
            categories.Select(item => item.Amount).ToList(),
            total,
            correctFinalPercentage: true);
        var breakdown = new List<DashboardSpendingBreakdownItemResponse>(categories.Count);

        for (int i = 0; i < categories.Count; i++)
        {
            CategoryAmount category = categories[i];

            breakdown.Add(new DashboardSpendingBreakdownItemResponse(
                category.Category,
                category.Amount,
                percentages[i],
                DashboardCalculations.GetColorKey(i)));
        }

        return breakdown;
    }

    private sealed record CategoryAmount(string Category, decimal Amount);

    internal sealed class RecentTransactionRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public string Merchant { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public decimal Amount { get; init; } = 0m;
        public string Type { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; } = DateTime.UnixEpoch;
    }
}
