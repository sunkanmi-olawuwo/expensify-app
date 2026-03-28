using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Users.Domain.Currencies;

namespace Expensify.Modules.Users.Infrastructure.Currencies.Configuration;

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(currency => currency.Id);

        builder.Property(currency => currency.Id)
            .HasColumnName("code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(currency => currency.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(currency => currency.Symbol)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(currency => currency.MinorUnit).IsRequired();
        builder.Property(currency => currency.IsActive).IsRequired();
        builder.Property(currency => currency.IsDefault).IsRequired();
        builder.Property(currency => currency.SortOrder).IsRequired();

        builder.HasIndex(currency => currency.IsDefault)
            .IsUnique()
            .HasFilter("\"is_default\" AND \"is_active\"");
    }
}
