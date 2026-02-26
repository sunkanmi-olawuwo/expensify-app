using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Tags;

public sealed class ExpenseTagUpdatedDomainEvent(Guid tagId) : DomainEvent
{
    public Guid TagId { get; init; } = tagId;
}
