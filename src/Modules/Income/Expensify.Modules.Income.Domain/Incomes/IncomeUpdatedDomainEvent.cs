using Expensify.Common.Domain;

namespace Expensify.Modules.Income.Domain.Incomes;

public sealed class IncomeUpdatedDomainEvent(Guid incomeId) : DomainEvent
{
    public Guid IncomeId { get; init; } = incomeId;
}
