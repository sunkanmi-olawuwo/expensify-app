namespace Expensify.Modules.Expenses.Domain.Policies;

public static class ExpensePolicyConsts
{
    public const string ReadPolicy = "expenses:read";
    public const string WritePolicy = "expenses:write";
    public const string DeletePolicy = "expenses:delete";
    public const string AdminReadPolicy = "admin:expenses:read";
}
