using FluentValidation;

namespace Expensify.Modules.Users.Application.Users.Command.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email address.");

        RuleFor(c => c.Token)
            .NotEmpty()
            .WithMessage("Reset token is required.");

        RuleFor(c => c.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required.");
    }
}
