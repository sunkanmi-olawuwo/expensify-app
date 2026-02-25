using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Expenses.Application.Categories.Command.DeleteExpenseCategory;

public sealed record DeleteExpenseCategoryCommand(Guid UserId, Guid CategoryId) : ICommand;
