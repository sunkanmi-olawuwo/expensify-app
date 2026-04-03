namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardIncomeBreakdown;

public sealed record DashboardIncomeBreakdownResponse(
    string Period,
    string Currency,
    decimal TotalIncome,
    IReadOnlyCollection<DashboardIncomeBreakdownSourceResponse> Sources);

public sealed record DashboardIncomeBreakdownSourceResponse(
    string Source,
    decimal Amount,
    decimal Percentage,
    string ColorKey);
