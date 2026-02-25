namespace Expensify.Modules.Income.Presentation;

internal static class RouteConsts
{
    private const string IncomeBase = "/income";

    internal const string Income = IncomeBase;
    internal const string IncomeById = $"{IncomeBase}/{{incomeId:guid}}";
    internal const string MonthlySummary = $"{IncomeBase}/summary/monthly";
    internal const string AdminIncome = $"{IncomeBase}/users/{{userId:guid}}";
    internal const string AdminMonthlySummary = $"{IncomeBase}/users/{{userId:guid}}/summary/monthly";
}
