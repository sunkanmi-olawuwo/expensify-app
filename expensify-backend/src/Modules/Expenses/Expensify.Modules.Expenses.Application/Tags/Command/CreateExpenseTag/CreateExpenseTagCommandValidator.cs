using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Tags.Command.CreateExpenseTag;

internal sealed class CreateExpenseTagCommandValidator : AbstractValidator<CreateExpenseTagCommand>
{
    public CreateExpenseTagCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(80);
    }
}
