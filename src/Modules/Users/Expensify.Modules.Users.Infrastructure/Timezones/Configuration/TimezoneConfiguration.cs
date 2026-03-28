using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Users.Domain.Timezones;

namespace Expensify.Modules.Users.Infrastructure.Timezones.Configuration;

internal sealed class TimezoneConfiguration : IEntityTypeConfiguration<Timezone>
{
    public void Configure(EntityTypeBuilder<Timezone> builder)
    {
        builder.ToTable("timezones");

        builder.HasKey(timezone => timezone.Id);

        builder.Property(timezone => timezone.Id)
            .HasColumnName("iana_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(timezone => timezone.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(timezone => timezone.IsActive).IsRequired();
        builder.Property(timezone => timezone.IsDefault).IsRequired();
        builder.Property(timezone => timezone.SortOrder).IsRequired();

        builder.HasIndex(timezone => timezone.IsDefault)
            .IsUnique()
            .HasFilter("\"is_default\" AND \"is_active\"");
    }
}
