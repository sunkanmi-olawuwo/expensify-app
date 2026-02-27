namespace Expensify.Modules.Income.Domain.Incomes;

public interface IIncomeRepository
{
    Task<Income?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Income?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Income income);

    void Update(Income income);
}
