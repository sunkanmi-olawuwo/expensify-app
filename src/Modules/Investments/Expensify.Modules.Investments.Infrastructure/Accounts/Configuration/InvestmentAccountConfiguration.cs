using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Investments.Domain.Accounts;
using Expensify.Modules.Investments.Domain.Categories;

namespace Expensify.Modules.Investments.Infrastructure.Accounts.Configuration;

internal sealed class InvestmentAccountConfiguration : IEntityTypeConfiguration<InvestmentAccount>
{
    public void Configure(EntityTypeBuilder<InvestmentAccount> builder)
    {
        builder.ToTable("investment_accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(a => a.Provider)
            .HasMaxLength(150);

        builder.Property(a => a.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(a => a.CurrentBalance)
            .HasPrecision(18, 2);

        builder.Property(a => a.InterestRate)
            .HasPrecision(8, 4);

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.Property(a => a.DeletedAtUtc);

        builder.HasIndex(a => new { a.UserId, a.CategoryId });
        builder.HasIndex(a => new { a.UserId, a.DeletedAtUtc });

        builder.HasOne<InvestmentCategory>()
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
