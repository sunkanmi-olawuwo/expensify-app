using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Infrastructure.Database;

namespace Expensify.Modules.Investments.Infrastructure.Accounts;

internal sealed class InvestmentAccountRepository(InvestmentsDbContext context)
    : Repository<InvestmentAccount, Guid>(context), IInvestmentAccountRepository
{
    public new async Task<InvestmentAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.InvestmentAccounts.SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<InvestmentAccount?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.InvestmentAccounts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
    }
}
