namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCategoryComparison;

public sealed record DashboardCategoryComparisonResponse(
    string CurrentMonth,
    string PreviousMonth,
    string Currency,
    IReadOnlyCollection<DashboardCategoryComparisonItemResponse> Categories);

public sealed record DashboardCategoryComparisonItemResponse(
    string Category,
    decimal CurrentAmount,
    decimal PreviousAmount,
    decimal ChangeAmount,
    decimal ChangePercentage);
