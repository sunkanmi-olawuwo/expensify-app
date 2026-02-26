using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Tags;

public sealed class ExpenseTag : Entity<Guid>, IAuditableEntity
{
    private ExpenseTag()
    {
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public static ExpenseTag Create(Guid userId, string name)
    {
        var tag = new ExpenseTag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim()
        };

        tag.Raise(new ExpenseTagCreatedDomainEvent(tag.Id));
        return tag;
    }

    public void Update(string name)
    {
        string normalizedName = name.Trim();
        if (Name == normalizedName)
        {
            return;
        }

        Name = normalizedName;
        Raise(new ExpenseTagUpdatedDomainEvent(Id));
    }

    public void RaiseDeletedEvent()
    {
        Raise(new ExpenseTagDeletedDomainEvent(Id));
    }
}
