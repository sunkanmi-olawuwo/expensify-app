namespace Expensify.Modules.Expenses.Domain.Categories;

public interface IExpenseCategoryRepository
{
    Task<ExpenseCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludedId, CancellationToken cancellationToken = default);

    void Add(ExpenseCategory category);

    void Update(ExpenseCategory category);

    void Remove(ExpenseCategory category);
}
