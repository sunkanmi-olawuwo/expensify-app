using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.RestoreExpense;

internal sealed class RestoreExpenseCommandValidator : AbstractValidator<RestoreExpenseCommand>
{
    public RestoreExpenseCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ExpenseId).NotEmpty();
    }
}