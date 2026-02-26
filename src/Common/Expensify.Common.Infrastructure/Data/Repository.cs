using Microsoft.EntityFrameworkCore;
using Expensify.Common.Domain;

namespace Expensify.Common.Infrastructure.Data;

public abstract class Repository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>
    where TEntityId : notnull
{
    protected readonly DbContext DbContext;

    protected Repository(DbContext context)
    {
        DbContext = context;
    }

    public async Task<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public void Add(TEntity entity)
    {
        DbContext.Set<TEntity>().Add(entity);
    }

    public void Update(TEntity entity)
    {
        EntityState state = DbContext.Entry(entity).State;
        if (state != EntityState.Detached)
        {
            return;
        }

        DbContext.Set<TEntity>().Attach(entity);
    }

    public void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
    }
}
