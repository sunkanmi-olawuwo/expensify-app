using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Expenses;

public sealed class ExpenseDeletedDomainEvent(Guid expenseId) : DomainEvent
{
    public Guid ExpenseId { get; init; } = expenseId;
}
