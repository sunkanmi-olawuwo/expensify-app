namespace Expensify.Common.Domain;

public interface IEntity
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}
