using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Infrastructure.Database;

namespace Expensify.Modules.Expenses.Infrastructure.Categories;

internal sealed class ExpenseCategoryRepository(ExpensesDbContext context)
    : Repository<ExpenseCategory, Guid>(context), IExpenseCategoryRepository
{
    public async Task<bool> ExistsByNameAsync(Guid userId, string name, Guid? excludedId, CancellationToken cancellationToken = default)
    {
        string normalizedName = name.Trim();

        IQueryable<ExpenseCategory> query = context.ExpenseCategories
            .Where(c => c.UserId == userId && c.Name == normalizedName);

        if (excludedId.HasValue)
        {
            query = query.Where(c => c.Id != excludedId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
