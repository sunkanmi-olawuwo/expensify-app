using Expensify.Common.Domain;

namespace Expensify.Modules.Income.Domain.Incomes;

public sealed class IncomeCreatedDomainEvent(Guid incomeId) : DomainEvent
{
    public Guid IncomeId { get; init; } = incomeId;
}
