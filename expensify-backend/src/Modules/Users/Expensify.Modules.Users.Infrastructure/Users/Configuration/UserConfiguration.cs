using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Users.Domain.Users;


namespace Expensify.Modules.Users.Infrastructure.Users.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName).HasMaxLength(200).IsRequired();

        builder.Property(u => u.LastName).HasMaxLength(200).IsRequired();

        builder.Property(u => u.IdentityId).HasMaxLength(450).IsRequired();

        builder.HasIndex(u => u.IdentityId).IsUnique();
    }
}
