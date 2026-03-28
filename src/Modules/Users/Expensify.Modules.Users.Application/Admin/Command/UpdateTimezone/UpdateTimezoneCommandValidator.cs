using FluentValidation;

namespace Expensify.Modules.Users.Application.Admin.Command.UpdateTimezone;

internal sealed class UpdateTimezoneCommandValidator : AbstractValidator<UpdateTimezoneCommand>
{
    public UpdateTimezoneCommandValidator()
    {
        RuleFor(c => c.IanaId).NotEmpty().MaximumLength(100);
        RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);
    }
}
