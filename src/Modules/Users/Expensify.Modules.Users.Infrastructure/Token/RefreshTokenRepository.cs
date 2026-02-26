using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Users.Domain.Tokens;
using Expensify.Modules.Users.Infrastructure.Database;

namespace Expensify.Modules.Users.Infrastructure.Token;

internal sealed class RefreshTokenRepository : Repository<RefreshToken, string>, IRefreshTokenRepository
{
    private readonly UsersDbContext _context;
    public RefreshTokenRepository(UsersDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<RefreshToken>> GetValidTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
       return await _context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && !rt.Invalidated)
            .ToListAsync(cancellationToken);
    }
}
