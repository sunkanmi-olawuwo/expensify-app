using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategory;

public sealed record GetExpenseCategoryQuery(Guid UserId, Guid CategoryId) : IQuery<ExpenseCategoryResponse>;
