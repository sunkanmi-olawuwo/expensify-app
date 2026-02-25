using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Tags.Command.UpdateExpenseTag;

internal sealed class UpdateExpenseTagCommandValidator : AbstractValidator<UpdateExpenseTagCommand>
{
    public UpdateExpenseTagCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.TagId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(80);
    }
}
