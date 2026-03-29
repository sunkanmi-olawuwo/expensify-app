using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Inbox;
using Expensify.Common.Infrastructure.Outbox;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;
using Expensify.Modules.Investments.Domain.Contributions;
using Expensify.Modules.Investments.Infrastructure.Accounts.Configuration;
using Expensify.Modules.Investments.Infrastructure.Categories.Configuration;
using Expensify.Modules.Investments.Infrastructure.Contributions.Configuration;

namespace Expensify.Modules.Investments.Infrastructure.Database;

public sealed class InvestmentsDbContext(DbContextOptions<InvestmentsDbContext> options)
    : DbContext(options), IInvestmentsUnitOfWork
{
    public DbSet<InvestmentAccount> InvestmentAccounts { get; set; }

    public DbSet<InvestmentCategory> InvestmentCategories { get; set; }

    public DbSet<InvestmentContribution> InvestmentContributions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Investments);

        modelBuilder.ApplyConfiguration(new InvestmentAccountConfiguration());
        modelBuilder.ApplyConfiguration(new InvestmentCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new InvestmentContributionConfiguration());

        modelBuilder.Entity<InvestmentAccount>().HasQueryFilter(a => a.DeletedAtUtc == null);
        modelBuilder.Entity<InvestmentContribution>().HasQueryFilter(c => c.DeletedAtUtc == null);

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConsumerConfiguration());
    }
}
