using FluentValidation;

namespace Expensify.Modules.Investments.Application.Contributions.Command.CreateInvestmentContribution;

internal sealed class CreateInvestmentContributionCommandValidator : AbstractValidator<CreateInvestmentContributionCommand>
{
    public CreateInvestmentContributionCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.InvestmentId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Notes).MaximumLength(1000);
    }
}
