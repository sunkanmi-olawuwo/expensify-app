using FluentValidation;

namespace Expensify.Modules.Income.Application.Incomes.Command.RestoreIncome;

internal sealed class RestoreIncomeCommandValidator : AbstractValidator<RestoreIncomeCommand>
{
    public RestoreIncomeCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.IncomeId).NotEmpty();
    }
}