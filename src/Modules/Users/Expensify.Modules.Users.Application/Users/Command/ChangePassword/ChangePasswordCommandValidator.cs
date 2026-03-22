using FluentValidation;

namespace Expensify.Modules.Users.Application.Users.Command.ChangePassword;

internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(c => c.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.");
    }
}
