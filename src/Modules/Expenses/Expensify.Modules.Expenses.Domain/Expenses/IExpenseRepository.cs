namespace Expensify.Modules.Expenses.Domain.Expenses;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCategoryAsync(Guid userId, Guid categoryId, CancellationToken cancellationToken = default);

    void Add(Expense expense);

    void Update(Expense expense);

    void Remove(Expense expense);
}
