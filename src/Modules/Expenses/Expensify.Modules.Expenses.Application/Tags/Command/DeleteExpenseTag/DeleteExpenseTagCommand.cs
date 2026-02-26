using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Expenses.Application.Tags.Command.DeleteExpenseTag;

public sealed record DeleteExpenseTagCommand(Guid UserId, Guid TagId) : ICommand;
