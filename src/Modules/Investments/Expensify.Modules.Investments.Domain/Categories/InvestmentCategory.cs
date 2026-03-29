using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Categories;

public sealed class InvestmentCategory : Entity<Guid>, IAuditableEntity
{
    private InvestmentCategory()
    {
    }

    public static InvestmentCategory Create(Guid id, string name, string slug, bool isActive)
    {
        return new InvestmentCategory
        {
            Id = id,
            Name = name.Trim(),
            Slug = slug.Trim(),
            IsActive = isActive
        };
    }

    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Raise(new InvestmentCategoryUpdatedDomainEvent(Id));
    }
}
