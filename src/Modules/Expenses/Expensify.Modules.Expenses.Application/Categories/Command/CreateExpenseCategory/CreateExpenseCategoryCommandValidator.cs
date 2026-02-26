using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Categories.Command.CreateExpenseCategory;

internal sealed class CreateExpenseCategoryCommandValidator : AbstractValidator<CreateExpenseCategoryCommand>
{
    public CreateExpenseCategoryCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(80);
    }
}
