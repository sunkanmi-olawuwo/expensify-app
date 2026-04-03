namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentTrend;

public sealed record DashboardInvestmentTrendResponse(
    string Currency,
    decimal TotalContributed,
    IReadOnlyCollection<DashboardInvestmentTrendMonthResponse> Months);

public sealed record DashboardInvestmentTrendMonthResponse(
    string Label,
    decimal Contributions,
    int ContributingAccountCount);
