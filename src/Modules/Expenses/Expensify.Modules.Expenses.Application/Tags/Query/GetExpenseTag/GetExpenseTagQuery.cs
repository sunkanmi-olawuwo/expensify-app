using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTag;

public sealed record GetExpenseTagQuery(Guid UserId, Guid TagId) : IQuery<ExpenseTagResponse>;
