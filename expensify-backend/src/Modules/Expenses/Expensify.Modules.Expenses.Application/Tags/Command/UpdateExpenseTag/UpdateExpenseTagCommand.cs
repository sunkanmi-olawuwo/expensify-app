using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Tags.Command.UpdateExpenseTag;

public sealed record UpdateExpenseTagCommand(Guid UserId, Guid TagId, string Name) : ICommand<ExpenseTagResponse>;
