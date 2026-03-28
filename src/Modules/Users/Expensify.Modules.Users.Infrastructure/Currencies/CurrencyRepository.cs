using Microsoft.EntityFrameworkCore;
using Expensify.Common.Infrastructure.Data;
using Expensify.Modules.Users.Domain.Currencies;
using Expensify.Modules.Users.Infrastructure.Database;

namespace Expensify.Modules.Users.Infrastructure.Currencies;

internal sealed class CurrencyRepository(UsersDbContext context)
    : Repository<Currency, string>(context), ICurrencyRepository
{
    public async Task<Currency?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await context.Currencies.SingleOrDefaultAsync(currency => currency.IsActive && currency.IsDefault, cancellationToken);
    }
}
