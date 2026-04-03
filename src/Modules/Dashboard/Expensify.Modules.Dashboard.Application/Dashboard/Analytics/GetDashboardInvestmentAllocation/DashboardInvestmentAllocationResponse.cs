namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentAllocation;

public sealed record DashboardInvestmentAllocationResponse(
    string Currency,
    decimal TotalValue,
    int AccountCount,
    IReadOnlyCollection<DashboardInvestmentAllocationCategoryResponse> Categories);

public sealed record DashboardInvestmentAllocationCategoryResponse(
    string CategoryName,
    string CategorySlug,
    decimal TotalBalance,
    int AccountCount,
    decimal Percentage,
    string ColorKey);
