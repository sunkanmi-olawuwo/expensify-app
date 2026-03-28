namespace Expensify.Modules.Dashboard.Application.Dashboard.GetDashboardSummary;

public sealed record DashboardSummaryResponse(
    DashboardMetricResponse MonthlyIncome,
    DashboardMetricResponse MonthlyExpenses,
    DashboardMetricResponse NetCashFlow,
    IReadOnlyCollection<DashboardSpendingBreakdownItemResponse> SpendingBreakdown,
    IReadOnlyCollection<DashboardMonthlyPerformanceItemResponse> MonthlyPerformance,
    IReadOnlyCollection<DashboardRecentTransactionResponse> RecentTransactions);

public sealed record DashboardMetricResponse(
    decimal TotalAmount,
    string Currency,
    decimal ChangePercentage);

public sealed record DashboardSpendingBreakdownItemResponse(
    string Category,
    decimal Amount,
    decimal Percentage,
    string ColorKey);

public sealed record DashboardMonthlyPerformanceItemResponse(
    string Month,
    decimal Income,
    decimal Expenses);

public sealed record DashboardRecentTransactionResponse(
    Guid Id,
    string Merchant,
    string Category,
    decimal Amount,
    string Type,
    string Status,
    DateTimeOffset Timestamp);
