using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Contributions;

public sealed class InvestmentContributionCreatedDomainEvent(Guid contributionId) : DomainEvent
{
    public Guid ContributionId { get; init; } = contributionId;
}
