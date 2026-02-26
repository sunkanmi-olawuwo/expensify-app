using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetExpense;

public sealed record GetExpenseQuery(Guid UserId, Guid ExpenseId) : IQuery<ExpenseResponse>;
