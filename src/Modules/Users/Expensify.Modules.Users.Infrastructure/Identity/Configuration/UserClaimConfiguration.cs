using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Expensify.Modules.Users.Domain.Identity;

namespace Expensify.Modules.Users.Infrastructure.Identity.Configuration;

public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("user_claims");
    }
}
