using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Tags.Command.DeleteExpenseTag;

internal sealed class DeleteExpenseTagCommandValidator : AbstractValidator<DeleteExpenseTagCommand>
{
    public DeleteExpenseTagCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.TagId).NotEmpty();
    }
}
