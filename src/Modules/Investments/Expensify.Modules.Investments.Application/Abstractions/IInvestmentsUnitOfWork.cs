namespace Expensify.Modules.Investments.Application.Abstractions;

public interface IInvestmentsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
