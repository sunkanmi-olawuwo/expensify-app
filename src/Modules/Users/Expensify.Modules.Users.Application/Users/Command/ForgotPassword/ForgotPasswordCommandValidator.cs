using FluentValidation;

namespace Expensify.Modules.Users.Application.Users.Command.ForgotPassword;

internal sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email address.");
    }
}
