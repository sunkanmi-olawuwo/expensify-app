namespace Expensify.Common.Domain;

public abstract class Entity<TEntityId> : IEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TEntityId Id { get; init; }

    protected Entity()
    {
    }

    protected Entity(TEntityId id)
    {
        Id = id;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
    {
        return [.. _domainEvents];
    }
}
