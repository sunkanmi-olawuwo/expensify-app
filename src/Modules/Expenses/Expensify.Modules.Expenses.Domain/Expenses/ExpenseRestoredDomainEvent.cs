using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Expenses;

public sealed class ExpenseRestoredDomainEvent(Guid expenseId) : DomainEvent
{
    public Guid ExpenseId { get; } = expenseId;
}