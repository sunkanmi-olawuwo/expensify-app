using FluentValidation;

namespace Expensify.Modules.Users.Application.Users.Command.RegisterUser;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(c => c.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(c => c.Email).EmailAddress().WithMessage("Invalid email address.");
        RuleFor(c => c.Password).MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
