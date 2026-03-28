using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Users.Domain.Timezones;
using Expensify.Modules.Users.Infrastructure.Database;

namespace Expensify.Modules.Users.Infrastructure.Timezones;

internal sealed class TimezoneRepository(UsersDbContext context)
    : Repository<Timezone, string>(context), ITimezoneRepository
{
    public async Task<Timezone?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await context.Timezones.SingleOrDefaultAsync(timezone => timezone.IsActive && timezone.IsDefault, cancellationToken);
    }
}
