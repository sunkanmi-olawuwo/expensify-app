using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Categories;

public sealed class ExpenseCategory : Entity<Guid>, IAuditableEntity
{
    private ExpenseCategory()
    {
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public static ExpenseCategory Create(Guid userId, string name)
    {
        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim()
        };

        category.Raise(new ExpenseCategoryCreatedDomainEvent(category.Id));
        return category;
    }

    public void Update(string name)
    {
        string normalizedName = name.Trim();
        if (Name == normalizedName)
        {
            return;
        }

        Name = normalizedName;
        Raise(new ExpenseCategoryUpdatedDomainEvent(Id));
    }

    public void RaiseDeletedEvent()
    {
        Raise(new ExpenseCategoryDeletedDomainEvent(Id));
    }
}
