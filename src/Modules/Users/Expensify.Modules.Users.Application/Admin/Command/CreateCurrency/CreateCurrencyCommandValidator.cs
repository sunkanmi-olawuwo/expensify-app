using FluentValidation;

namespace Expensify.Modules.Users.Application.Admin.Command.CreateCurrency;

internal sealed class CreateCurrencyCommandValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(c => c.Code).Matches("^[A-Za-z]{3}$");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Symbol).NotEmpty().MaximumLength(10);
        RuleFor(c => c.MinorUnit).InclusiveBetween(0, 9);
    }
}
