using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategories;

public sealed record GetExpenseCategoriesQuery(Guid UserId) : IQuery<IReadOnlyCollection<ExpenseCategoryResponse>>;
