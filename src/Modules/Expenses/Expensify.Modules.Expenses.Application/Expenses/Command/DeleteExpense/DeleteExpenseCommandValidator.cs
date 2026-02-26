using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.DeleteExpense;

internal sealed class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
{
    public DeleteExpenseCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ExpenseId).NotEmpty();
    }
}
