namespace Expensify.Modules.Income.Application.Abstractions;

public interface IIncomeUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
