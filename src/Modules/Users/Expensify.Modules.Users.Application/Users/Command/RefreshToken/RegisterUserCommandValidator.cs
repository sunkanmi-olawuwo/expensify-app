using FluentValidation;

namespace Expensify.Modules.Users.Application.Users.Command.RefreshToken;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
        RuleFor(c => c.Token).NotEmpty();
    }
}
