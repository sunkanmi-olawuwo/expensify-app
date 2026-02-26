namespace Expensify.Modules.Expenses.Presentation;

internal static class RouteConsts
{
    private const string ExpensesBase = "/expenses";
    private const string CategoriesBase = "/expense-categories";
    private const string TagsBase = "/expense-tags";

    internal const string Expenses = ExpensesBase;
    internal const string ExpenseById = $"{ExpensesBase}/{{expenseId:guid}}";
    internal const string MonthlySummary = $"{ExpensesBase}/summary/monthly";
    internal const string AdminExpenses = $"{ExpensesBase}/users/{{userId:guid}}";
    internal const string AdminMonthlySummary = $"{ExpensesBase}/users/{{userId:guid}}/summary/monthly";

    internal const string Categories = CategoriesBase;
    internal const string CategoryById = $"{CategoriesBase}/{{categoryId:guid}}";

    internal const string Tags = TagsBase;
    internal const string TagById = $"{TagsBase}/{{tagId:guid}}";
}
