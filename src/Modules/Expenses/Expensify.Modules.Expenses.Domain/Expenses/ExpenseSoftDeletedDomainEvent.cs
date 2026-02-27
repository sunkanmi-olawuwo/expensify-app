using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Expenses;

public sealed class ExpenseSoftDeletedDomainEvent(Guid expenseId) : DomainEvent
{
    public Guid ExpenseId { get; } = expenseId;
}