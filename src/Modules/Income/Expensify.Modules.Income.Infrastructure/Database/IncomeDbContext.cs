using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Inbox;
using Expensify.Common.Infrastructure.Outbox;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Domain.Incomes;
using Expensify.Modules.Income.Infrastructure.Incomes.Configuration;
using IncomeEntity = Expensify.Modules.Income.Domain.Incomes.Income;

namespace Expensify.Modules.Income.Infrastructure.Database;

public sealed class IncomeDbContext(DbContextOptions<IncomeDbContext> options)
    : DbContext(options), IIncomeUnitOfWork
{
    public DbSet<IncomeEntity> Incomes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Income);

        modelBuilder.ApplyConfiguration(new IncomeConfiguration());

        modelBuilder.Entity<IncomeEntity>().HasQueryFilter(i => i.DeletedAtUtc == null);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
    }
}
