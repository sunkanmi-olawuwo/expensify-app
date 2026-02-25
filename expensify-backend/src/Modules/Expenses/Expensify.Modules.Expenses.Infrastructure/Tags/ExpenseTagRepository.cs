using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Expenses.Domain.Tags;
using Expensify.Modules.Expenses.Infrastructure.Database;

namespace Expensify.Modules.Expenses.Infrastructure.Tags;

internal sealed class ExpenseTagRepository(ExpensesDbContext context)
    : Repository<ExpenseTag, Guid>(context), IExpenseTagRepository
{
    public async Task<IReadOnlyCollection<ExpenseTag>> GetByIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        Guid[] distinctIds = ids.Distinct().ToArray();
        if (distinctIds.Length == 0)
        {
            return [];
        }

        return await context.ExpenseTags
            .Where(t => t.UserId == userId && distinctIds.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludedId, CancellationToken cancellationToken = default)
    {
        string normalizedName = name.Trim();

        IQueryable<ExpenseTag> query = context.ExpenseTags
            .Where(t => t.UserId == userId && t.Name == normalizedName);

        if (excludedId.HasValue)
        {
            query = query.Where(t => t.Id != excludedId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
