using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Infrastructure.Database;

namespace Expensify.Modules.Expenses.Infrastructure.Expenses;

internal sealed class ExpenseRepository(ExpensesDbContext context)
    : Repository<Expense, Guid>(context), IExpenseRepository
{
    public new async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Expenses
            .Include(e => e.Tags)
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
}
