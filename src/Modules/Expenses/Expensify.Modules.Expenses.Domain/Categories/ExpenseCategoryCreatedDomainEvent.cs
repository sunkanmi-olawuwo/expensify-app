using Expensify.Common.Domain;

namespace Expensify.Modules.Expenses.Domain.Categories;

public sealed class ExpenseCategoryCreatedDomainEvent(Guid categoryId) : DomainEvent
{
    public Guid CategoryId { get; init; } = categoryId;
}
