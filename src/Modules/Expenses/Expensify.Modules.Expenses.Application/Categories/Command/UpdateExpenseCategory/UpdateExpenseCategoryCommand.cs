using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Categories.Command.UpdateExpenseCategory;

public sealed record UpdateExpenseCategoryCommand(Guid UserId, Guid CategoryId, string Name) : ICommand<ExpenseCategoryResponse>;
