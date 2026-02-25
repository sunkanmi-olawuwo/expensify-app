using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.UpdateExpense;

internal sealed class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ExpenseId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).Matches("^[A-Z]{3}$");
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Merchant).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Note).MaximumLength(1000);
    }
}
