using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Expenses;

public static class ExpenseErrors
{
    private const string Prefix = "Expenses";

    public static Error NotFound(Guid expenseId) =>
        Error.NotFound($"{Prefix}.NotFound", $"The expense with identifier {expenseId} was not found");

    public static Error CategoryNotFound(Guid categoryId) =>
        Error.NotFound($"{Prefix}.CategoryNotFound", $"The expense category with identifier {categoryId} was not found");

    public static Error TagNotFound(Guid tagId) =>
        Error.NotFound($"{Prefix}.TagNotFound", $"The expense tag with identifier {tagId} was not found");

    public static Error CategoryInUse(Guid categoryId) =>
        Error.Validation($"{Prefix}.CategoryInUse", $"The expense category with identifier {categoryId} is referenced by one or more expenses");

    public static Error InvalidAmount() =>
        Error.Validation($"{Prefix}.InvalidAmount", "Amount must be greater than zero");

    public static Error InvalidCurrency(string? currency) =>
        Error.Validation($"{Prefix}.InvalidCurrency", $"Currency '{currency}' must be a 3-letter uppercase ISO code");

    public static Error CurrencyMismatch(string expectedCurrency, string actualCurrency) =>
        Error.Validation($"{Prefix}.CurrencyMismatch", $"Expense currency '{actualCurrency}' does not match user currency '{expectedCurrency}'");

    public static Error OwnershipMismatch() =>
        Error.Validation($"{Prefix}.OwnershipMismatch", "Expense references category or tags that do not belong to the user");

    public static Error AlreadyDeleted(Guid expenseId) =>
        Error.Problem($"{Prefix}.AlreadyDeleted", $"The expense with identifier {expenseId} is already deleted");

    public static Error NotDeleted(Guid expenseId) =>
        Error.Problem($"{Prefix}.NotDeleted", $"The expense with identifier {expenseId} is not deleted");

    public static Error PeriodInvalid(string period) =>
        Error.Validation($"{Prefix}.InvalidPeriod", $"Period '{period}' must be in YYYY-MM format");
}
