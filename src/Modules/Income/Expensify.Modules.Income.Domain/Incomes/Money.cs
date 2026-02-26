using Expensify.Common.Domain;

namespace Expensify.Modules.Income.Domain.Incomes;

public readonly record struct Money(decimal Amount, string Currency)
{
    public Result Validate()
    {
        if (Amount <= 0)
        {
            return Result.Failure(IncomeErrors.InvalidAmount());
        }

        if (string.IsNullOrWhiteSpace(Currency) ||
            Currency.Length != 3 ||
            Currency.Any(character => !char.IsLetter(character) || !char.IsUpper(character)))
        {
            return Result.Failure(IncomeErrors.InvalidCurrency(Currency));
        }

        return Result.Success();
    }
}
