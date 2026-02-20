using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Expensify.Common.Domain;

namespace Expensify.Common.Infrastructure.Data;

public class AuditableInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        DbContext context = eventData.Context!;
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IAuditableEntity>> entries = context.ChangeTracker.Entries<IAuditableEntity>();

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IAuditableEntity> entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
