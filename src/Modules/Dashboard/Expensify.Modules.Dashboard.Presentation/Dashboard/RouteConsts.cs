namespace Expensify.Modules.Dashboard.Presentation.Dashboard;

internal static class RouteConsts
{
    private const string DashboardBase = "/dashboard";
    private const string AnalyticsBase = $"{DashboardBase}/analytics";

    internal const string Summary = $"{DashboardBase}/summary";
    internal const string CashFlowTrend = $"{AnalyticsBase}/cash-flow-trend";
    internal const string IncomeBreakdown = $"{AnalyticsBase}/income-breakdown";
    internal const string CategoryComparison = $"{AnalyticsBase}/category-comparison";
    internal const string TopCategories = $"{AnalyticsBase}/top-categories";
    internal const string InvestmentAllocation = $"{AnalyticsBase}/investment-allocation";
    internal const string InvestmentTrend = $"{AnalyticsBase}/investment-trend";
}
