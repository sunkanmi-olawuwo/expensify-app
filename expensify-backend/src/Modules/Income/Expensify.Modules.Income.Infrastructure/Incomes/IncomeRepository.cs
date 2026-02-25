using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Infrastructure.Database;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Infrastructure.Incomes;

internal sealed class IncomeRepository(IncomeDbContext context)
    : Repository<IncomeEntity, Guid>(context), IIncomeRepository
{
    public new async Task<IncomeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Incomes
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
    }
}
