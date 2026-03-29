using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Investments.Domain.Categories;
using Expensify.Modules.Investments.Infrastructure.Database;

namespace Expensify.Modules.Investments.Infrastructure.Categories;

internal sealed class InvestmentCategoryRepository(InvestmentsDbContext context)
    : Repository<InvestmentCategory, Guid>(context), IInvestmentCategoryRepository
{
    public new async Task<InvestmentCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.InvestmentCategories.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
