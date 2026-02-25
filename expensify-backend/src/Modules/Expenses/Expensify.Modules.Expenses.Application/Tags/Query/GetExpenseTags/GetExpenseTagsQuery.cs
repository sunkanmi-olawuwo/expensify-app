using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTags;

public sealed record GetExpenseTagsQuery(Guid UserId) : IQuery<IReadOnlyCollection<ExpenseTagResponse>>;
