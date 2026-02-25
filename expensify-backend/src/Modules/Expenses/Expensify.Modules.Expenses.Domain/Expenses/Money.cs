using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Expenses;

public readonly record struct Money(decimal Amount, string Currency)
{
    public Result Validate()
    {
        if (Amount <= 0)
        {
            return Result.Failure(ExpenseErrors.InvalidAmount());
        }

        if (string.IsNullOrWhiteSpace(Currency) ||
            Currency.Length != 3 ||
            Currency.Any(character => !char.IsLetter(character) || !char.IsUpper(character)))
        {
            return Result.Failure(ExpenseErrors.InvalidCurrency(Currency));
        }

        return Result.Success();
    }
}
