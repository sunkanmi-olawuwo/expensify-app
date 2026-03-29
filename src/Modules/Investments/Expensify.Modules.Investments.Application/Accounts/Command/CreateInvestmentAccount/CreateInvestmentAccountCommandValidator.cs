using FluentValidation;

namespace Expensify.Modules.Investments.Application.Accounts.Command.CreateInvestmentAccount;

internal sealed class CreateInvestmentAccountCommandValidator : AbstractValidator<CreateInvestmentAccountCommand>
{
    public CreateInvestmentAccountCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Provider).MaximumLength(150);
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Currency).Matches("^[A-Z]{3}$");
        RuleFor(c => c.CurrentBalance).GreaterThanOrEqualTo(0);
        RuleFor(c => c.InterestRate).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100).When(c => c.InterestRate.HasValue);
        RuleFor(c => c.Notes).MaximumLength(1000);
    }
}
