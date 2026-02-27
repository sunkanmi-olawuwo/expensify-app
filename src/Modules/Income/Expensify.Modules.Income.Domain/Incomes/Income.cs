using Expensify.Common.Domain;

namespace Expensify.Modules.Income.Domain.Incomes;

public sealed class Income : Entity<Guid>, IAuditableEntity
{
    private Income()
    {
    }

    public Guid UserId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public DateOnly IncomeDate { get; private set; }

    public string Source { get; private set; }

    public IncomeType Type { get; private set; }

    public string Note { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public static Result<Income> Create(
        Guid userId,
        Money money,
        IncomeDate incomeDate,
        string? source,
        IncomeType type,
        string? note,
        string userCurrency)
    {
        Result moneyValidation = money.Validate();
        if (moneyValidation.IsFailure)
        {
            return Result.Failure<Income>(moneyValidation.Error);
        }

        if (!string.Equals(money.Currency, userCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<Income>(IncomeErrors.CurrencyMismatch(userCurrency, money.Currency));
        }

        var income = new Income
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = money.Amount,
            Currency = money.Currency,
            IncomeDate = incomeDate.Value,
            Source = source?.Trim() ?? string.Empty,
            Type = type,
            Note = note?.Trim() ?? string.Empty
        };

        income.Raise(new IncomeCreatedDomainEvent(income.Id));

        return income;
    }

    public Result Update(
        Money money,
        IncomeDate incomeDate,
        string? source,
        IncomeType type,
        string? note,
        string userCurrency)
    {
        Result moneyValidation = money.Validate();
        if (moneyValidation.IsFailure)
        {
            return moneyValidation;
        }

        if (!string.Equals(money.Currency, userCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(IncomeErrors.CurrencyMismatch(userCurrency, money.Currency));
        }

        Amount = money.Amount;
        Currency = money.Currency;
        IncomeDate = incomeDate.Value;
        Source = source?.Trim() ?? string.Empty;
        Type = type;
        Note = note?.Trim() ?? string.Empty;

        Raise(new IncomeUpdatedDomainEvent(Id));

        return Result.Success();
    }

    public void RaiseDeletedEvent()
    {
        Raise(new IncomeDeletedDomainEvent(Id));
    }

    public bool IsDeleted => DeletedAtUtc.HasValue;

    public Result MarkDeleted(DateTime deletedAtUtc)
    {
        if (DeletedAtUtc.HasValue)
        {
            return Result.Failure(IncomeErrors.AlreadyDeleted(Id));
        }

        DeletedAtUtc = deletedAtUtc;
        Raise(new IncomeSoftDeletedDomainEvent(Id));
        return Result.Success();
    }

    public Result Restore()
    {
        if (!DeletedAtUtc.HasValue)
        {
            return Result.Failure(IncomeErrors.NotDeleted(Id));
        }

        DeletedAtUtc = null;
        Raise(new IncomeRestoredDomainEvent(Id));
        return Result.Success();
    }
}
