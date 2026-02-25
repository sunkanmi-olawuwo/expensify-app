namespace Expensify.Modules.Expenses.Domain.Tags;

public interface IExpenseTagRepository
{
    Task<ExpenseTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExpenseTag>> GetByIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludedId, CancellationToken cancellationToken = default);

    void Add(ExpenseTag tag);

    void Update(ExpenseTag tag);

    void Remove(ExpenseTag tag);
}
