namespace Expensify.Modules.Investments.Presentation;

internal static class RouteConsts
{
    private const string InvestmentsBase = "/investments";
    private const string AdminInvestmentsBase = "/admin/investments";

    internal const string Investments = InvestmentsBase;
    internal const string InvestmentById = $"{InvestmentsBase}/{{investmentId:guid}}";
    internal const string Contributions = $"{InvestmentsBase}/{{investmentId:guid}}/contributions";
    internal const string Summary = $"{InvestmentsBase}/summary";
    internal const string Categories = $"{InvestmentsBase}/categories";
    internal const string AdminCategories = $"{AdminInvestmentsBase}/categories";
    internal const string AdminCategoryById = $"{AdminInvestmentsBase}/categories/{{categoryId:guid}}";
    internal const string AdminInvestments = AdminInvestmentsBase;
}
