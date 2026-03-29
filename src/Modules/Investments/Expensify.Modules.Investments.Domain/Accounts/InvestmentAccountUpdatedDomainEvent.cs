using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Accounts;

public sealed class InvestmentAccountUpdatedDomainEvent(Guid investmentId) : DomainEvent
{
    public Guid InvestmentId { get; init; } = investmentId;
}
