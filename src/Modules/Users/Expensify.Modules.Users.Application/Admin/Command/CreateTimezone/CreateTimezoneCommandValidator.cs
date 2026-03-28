using FluentValidation;

namespace Expensify.Modules.Users.Application.Admin.Command.CreateTimezone;

internal sealed class CreateTimezoneCommandValidator : AbstractValidator<CreateTimezoneCommand>
{
    public CreateTimezoneCommandValidator()
    {
        RuleFor(c => c.IanaId).NotEmpty().MaximumLength(100);
        RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);
    }
}
