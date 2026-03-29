using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Contributions;

namespace Expensify.Modules.Investments.Infrastructure.Contributions.Configuration;

internal sealed class InvestmentContributionConfiguration : IEntityTypeConfiguration<InvestmentContribution>
{
    public void Configure(EntityTypeBuilder<InvestmentContribution> builder)
    {
        builder.ToTable("investment_contributions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Amount)
            .HasPrecision(18, 2);

        builder.Property(c => c.Notes)
            .HasMaxLength(1000);

        builder.Property(c => c.DeletedAtUtc);

        builder.HasIndex(c => new { c.InvestmentId, c.Date });
        builder.HasIndex(c => new { c.InvestmentId, c.DeletedAtUtc });

        builder.HasOne<InvestmentAccount>()
            .WithMany()
            .HasForeignKey(c => c.InvestmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
