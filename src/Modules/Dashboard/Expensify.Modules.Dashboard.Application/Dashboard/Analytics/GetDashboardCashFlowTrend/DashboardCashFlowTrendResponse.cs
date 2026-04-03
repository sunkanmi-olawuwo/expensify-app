namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCashFlowTrend;

public sealed record DashboardCashFlowTrendResponse(
    IReadOnlyCollection<DashboardCashFlowTrendMonthResponse> Months,
    string Currency);

public sealed record DashboardCashFlowTrendMonthResponse(
    string Label,
    decimal Income,
    decimal Expenses,
    decimal NetCashFlow,
    decimal SavingsRate);
