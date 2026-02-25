using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Tags.Command.CreateExpenseTag;

public sealed record CreateExpenseTagCommand(Guid UserId, string Name) : ICommand<ExpenseTagResponse>;
