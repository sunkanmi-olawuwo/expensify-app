namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardTopCategories;

public sealed record DashboardTopCategoriesResponse(
    string Period,
    string Currency,
    decimal TotalSpent,
    IReadOnlyCollection<DashboardTopCategoryResponse> Categories);

public sealed record DashboardTopCategoryResponse(
    int Rank,
    string Category,
    decimal Amount,
    decimal Percentage,
    string ColorKey);
