using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Expensify.Common.Application.Data;
using Expensify.Common.Infrastructure.Inbox;
using Expensify.Common.Infrastructure.Outbox;
using Expensify.Modules.Users.Domain.Identity;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure.Identity.Configuration;
using Expensify.Modules.Users.Infrastructure.Token.Configuration;
using Expensify.Modules.Users.Infrastructure.Users.Configuration;

namespace Expensify.Modules.Users.Infrastructure.Database;

public class UsersDbContext : IdentityDbContext<IdentityUser, Role, string,
    UserClaim, UserRole, UserLogin,
    RoleClaim, UserToken>, IUnitOfWork
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public new DbSet<User> Users { get; set; }

    public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(Schemas.Users);

        builder.ApplyConfiguration(new UserConfiguration());

        builder.ApplyConfiguration(new RefreshTokenConfiguration());

        builder.ApplyConfiguration(new RoleClaimConfiguration());
        builder.ApplyConfiguration(new RoleConfiguration());
        builder.ApplyConfiguration(new IdentityUserConfiguration());
        builder.ApplyConfiguration(new UserClaimConfiguration());
        builder.ApplyConfiguration(new UserLoginConfiguration());
        builder.ApplyConfiguration(new UserRoleConfiguration());   
        builder.ApplyConfiguration(new UserTokenConfiguration());

        builder.ApplyConfiguration(new OutboxMessageConfiguration());
        builder.ApplyConfiguration(new OutboxMessageConsumerConfiguration());
        builder.ApplyConfiguration(new InboxMessageConfiguration());
        builder.ApplyConfiguration(new InboxMessageConsumerConfiguration());

    }
}
