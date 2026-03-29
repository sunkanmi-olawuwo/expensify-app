using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Investments.Domain.Categories;

namespace Expensify.Modules.Investments.Infrastructure.Categories.Configuration;

internal sealed class InvestmentCategoryConfiguration : IEntityTypeConfiguration<InvestmentCategory>
{
    public void Configure(EntityTypeBuilder<InvestmentCategory> builder)
    {
        builder.ToTable("investment_categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Slug)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasIndex(c => c.Slug)
            .IsUnique();
    }
}
