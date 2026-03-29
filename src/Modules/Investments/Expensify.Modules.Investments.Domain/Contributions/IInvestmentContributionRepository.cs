namespace Expensify.Modules.Investments.Domain.Contributions;

public interface IInvestmentContributionRepository
{
    Task<IReadOnlyCollection<InvestmentContribution>> GetByInvestmentIdIncludingDeletedAsync(Guid investmentId, CancellationToken cancellationToken = default);

    Task<decimal> GetTotalContributedAsync(Guid investmentId, CancellationToken cancellationToken = default);

    void Add(InvestmentContribution contribution);

    void Update(InvestmentContribution contribution);
}
