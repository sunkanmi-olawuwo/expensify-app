using FluentValidation;

namespace Expensify.Modules.Investments.Application.Accounts.Command.DeleteInvestmentAccount;

internal sealed class DeleteInvestmentAccountCommandValidator : AbstractValidator<DeleteInvestmentAccountCommand>
{
    public DeleteInvestmentAccountCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.InvestmentId).NotEmpty();
    }
}
