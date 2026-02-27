using Expensify.Common.Domain;

namespace Expensify.Modules.Income.Domain.Incomes;

public sealed class IncomeSoftDeletedDomainEvent(Guid incomeId) : DomainEvent
{
    public Guid IncomeId { get; } = incomeId;
}