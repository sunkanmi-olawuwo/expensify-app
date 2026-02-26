using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.DeleteExpense;

public sealed record DeleteExpenseCommand(Guid UserId, Guid ExpenseId) : ICommand;
