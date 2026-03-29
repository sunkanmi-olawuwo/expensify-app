using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Investments.Domain.Contributions;
using Expensify.Modules.Investments.Infrastructure.Database;

namespace Expensify.Modules.Investments.Infrastructure.Contributions;

internal sealed class InvestmentContributionRepository(InvestmentsDbContext context)
    : Repository<InvestmentContribution, Guid>(context), IInvestmentContributionRepository
{
    public async Task<IReadOnlyCollection<InvestmentContribution>> GetByInvestmentIdIncludingDeletedAsync(
        Guid investmentId,
        CancellationToken cancellationToken = default)
    {
        return await context.InvestmentContributions
            .IgnoreQueryFilters()
            .Where(c => c.InvestmentId == investmentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalContributedAsync(Guid investmentId, CancellationToken cancellationToken = default)
    {
        return await context.InvestmentContributions
            .Where(c => c.InvestmentId == investmentId)
            .SumAsync(c => (decimal?)c.Amount, cancellationToken) ?? 0m;
    }
}
