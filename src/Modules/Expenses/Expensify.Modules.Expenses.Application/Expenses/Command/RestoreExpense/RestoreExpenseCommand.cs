using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.RestoreExpense;

public sealed record RestoreExpenseCommand(Guid UserId, Guid ExpenseId) : ICommand;