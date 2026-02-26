using FluentValidation;

namespace Expensify.Modules.Users.Application.Users.Command.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).EmailAddress().WithMessage("Invalid email address.");
        RuleFor(c => c.Password).NotEmpty().WithMessage("Password is required.");
    }
}
