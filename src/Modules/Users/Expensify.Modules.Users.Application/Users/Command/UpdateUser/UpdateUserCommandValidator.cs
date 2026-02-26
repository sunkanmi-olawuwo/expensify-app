using FluentValidation;

namespace Expensify.Modules.Users.Application.Users.Command.UpdateUser;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    private const int MaxTimezoneLength = 100;

    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.Currency)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter uppercase code.");
        RuleFor(c => c.Timezone)
            .NotEmpty()
            .MaximumLength(MaxTimezoneLength);
        RuleFor(c => c.MonthStartDay)
            .InclusiveBetween(1, 28);
    }
}
