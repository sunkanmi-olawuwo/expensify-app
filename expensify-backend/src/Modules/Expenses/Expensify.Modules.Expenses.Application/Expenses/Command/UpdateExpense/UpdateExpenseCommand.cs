using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.UpdateExpense;

public sealed record UpdateExpenseCommand(
    Guid UserId,
    Guid ExpenseId,
    decimal Amount,
    string Currency,
    DateOnly Date,
    Guid CategoryId,
    string Merchant,
    string Note,
    PaymentMethod PaymentMethod,
    IReadOnlyCollection<Guid> TagIds) : ICommand<ExpenseResponse>;
