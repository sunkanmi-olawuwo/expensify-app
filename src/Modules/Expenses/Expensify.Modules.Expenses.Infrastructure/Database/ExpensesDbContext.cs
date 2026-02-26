using Microsoft.EntityFrameworkCore;
using Expensify.Common.Application.Data;
using Expensify.Common.Infrastructure.Inbox;
using Expensify.Common.Infrastructure.Outbox;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;
using Expensify.Modules.Expenses.Infrastructure.Categories.Configuration;
using Expensify.Modules.Expenses.Infrastructure.Expenses.Configuration;
using Expensify.Modules.Expenses.Infrastructure.Tags.Configuration;

namespace Expensify.Modules.Expenses.Infrastructure.Database;

public sealed class ExpensesDbContext(DbContextOptions<ExpensesDbContext> options)
    : DbContext(options), IExpensesUnitOfWork
{
    public DbSet<Expense> Expenses { get; set; }

    public DbSet<ExpenseCategory> ExpenseCategories { get; set; }

    public DbSet<ExpenseTag> ExpenseTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Expenses);

        modelBuilder.ApplyConfiguration(new ExpenseConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ExpenseTagConfiguration());

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
    }
}
