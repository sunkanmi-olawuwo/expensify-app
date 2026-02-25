using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Users.Domain.Users;


namespace Expensify.Modules.Users.Infrastructure.Users.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", tableBuilder =>
            tableBuilder.HasCheckConstraint("ck_users_month_start_day", "month_start_day >= 1 AND month_start_day <= 28"));

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName).HasMaxLength(200).IsRequired();

        builder.Property(u => u.LastName).HasMaxLength(200).IsRequired();

        builder.Property(u => u.IdentityId).HasMaxLength(450).IsRequired();

        builder.Property(u => u.Currency).HasMaxLength(3).IsRequired();

        builder.Property(u => u.Timezone).HasMaxLength(100).IsRequired();

        builder.Property(u => u.MonthStartDay).IsRequired();

        builder.HasIndex(u => u.IdentityId).IsUnique();
    }
}
