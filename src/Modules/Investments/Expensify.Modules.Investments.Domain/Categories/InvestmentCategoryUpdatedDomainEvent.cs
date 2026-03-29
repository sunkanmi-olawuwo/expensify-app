using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Categories;

public sealed class InvestmentCategoryUpdatedDomainEvent(Guid categoryId) : DomainEvent
{
    public Guid CategoryId { get; init; } = categoryId;
}
