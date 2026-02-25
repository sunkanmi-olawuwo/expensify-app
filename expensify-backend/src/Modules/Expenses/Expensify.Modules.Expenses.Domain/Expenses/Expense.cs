using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Domain.Expenses;

public sealed class Expense : Entity<Guid>, IAuditableEntity
{
    private readonly List<ExpenseTag> _tags = [];

    private Expense()
    {
    }

    public Guid UserId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public DateOnly ExpenseDate { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Merchant { get; private set; }

    public string Note { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public IReadOnlyCollection<ExpenseTag> Tags => _tags;

    public static Result<Expense> Create(
        Guid userId,
        Money money,
        ExpenseDate expenseDate,
        Guid categoryId,
        string merchant,
        string note,
        PaymentMethod paymentMethod,
        IReadOnlyCollection<ExpenseTag> tags,
        string userCurrency)
    {
        Result moneyValidation = money.Validate();
        if (moneyValidation.IsFailure)
        {
            return Result.Failure<Expense>(moneyValidation.Error);
        }

        if (!string.Equals(money.Currency, userCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<Expense>(ExpenseErrors.CurrencyMismatch(userCurrency, money.Currency));
        }

        if (tags.Any(t => t.UserId != userId))
        {
            return Result.Failure<Expense>(ExpenseErrors.OwnershipMismatch());
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = money.Amount,
            Currency = money.Currency,
            ExpenseDate = expenseDate.Value,
            CategoryId = categoryId,
            Merchant = merchant.Trim(),
            Note = note.Trim(),
            PaymentMethod = paymentMethod
        };

        expense._tags.AddRange(tags);
        expense.Raise(new ExpenseCreatedDomainEvent(expense.Id));

        return expense;
    }

    public Result Update(
        Money money,
        ExpenseDate expenseDate,
        Guid categoryId,
        string merchant,
        string note,
        PaymentMethod paymentMethod,
        IReadOnlyCollection<ExpenseTag> tags,
        string userCurrency)
    {
        Result moneyValidation = money.Validate();
        if (moneyValidation.IsFailure)
        {
            return moneyValidation;
        }

        if (!string.Equals(money.Currency, userCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(ExpenseErrors.CurrencyMismatch(userCurrency, money.Currency));
        }

        if (tags.Any(t => t.UserId != UserId))
        {
            return Result.Failure(ExpenseErrors.OwnershipMismatch());
        }

        Amount = money.Amount;
        Currency = money.Currency;
        ExpenseDate = expenseDate.Value;
        CategoryId = categoryId;
        Merchant = merchant.Trim();
        Note = note.Trim();
        PaymentMethod = paymentMethod;
        _tags.Clear();
        _tags.AddRange(tags);

        Raise(new ExpenseUpdatedDomainEvent(Id));

        return Result.Success();
    }

    public void RaiseDeletedEvent()
    {
        Raise(new ExpenseDeletedDomainEvent(Id));
    }
}
