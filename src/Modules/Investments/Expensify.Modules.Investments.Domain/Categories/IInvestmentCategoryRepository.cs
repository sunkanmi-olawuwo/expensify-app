namespace Expensify.Modules.Investments.Domain.Categories;

public interface IInvestmentCategoryRepository
{
    Task<InvestmentCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Update(InvestmentCategory category);
}
