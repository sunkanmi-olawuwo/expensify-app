namespace Expensify.Modules.Investments.Domain.Accounts;

public interface IInvestmentAccountRepository
{
    Task<InvestmentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvestmentAccount?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(InvestmentAccount account);

    void Update(InvestmentAccount account);
}
