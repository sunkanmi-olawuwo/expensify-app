using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Categories.Command.DeleteExpenseCategory;

internal sealed class DeleteExpenseCategoryCommandValidator : AbstractValidator<DeleteExpenseCategoryCommand>
{
    public DeleteExpenseCategoryCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.CategoryId).NotEmpty();
    }
}
