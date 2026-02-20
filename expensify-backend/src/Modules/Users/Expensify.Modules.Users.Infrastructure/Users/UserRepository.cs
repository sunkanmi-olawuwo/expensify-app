using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.Infrastructure.Database;

namespace Expensify.Modules.Users.Infrastructure.Users;

internal sealed class UserRepository : Repository<User, Guid>, IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdentityIdAsync(string identityId, CancellationToken cancellationToken = default)
    {
        return await _context.Users.SingleOrDefaultAsync(u => u.IdentityId == identityId, cancellationToken);
    }
}
