using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Categories.Command.CreateExpenseCategory;

public sealed record CreateExpenseCategoryCommand(Guid UserId, string Name) : ICommand<ExpenseCategoryResponse>;
