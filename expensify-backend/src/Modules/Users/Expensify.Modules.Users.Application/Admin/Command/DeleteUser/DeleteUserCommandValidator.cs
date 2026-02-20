using FluentValidation;
namespace Expensify.Modules.Users.Application.Admin.Command.DeleteUser;

internal sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty().WithMessage("User ID must not be empty.");
    }
}
