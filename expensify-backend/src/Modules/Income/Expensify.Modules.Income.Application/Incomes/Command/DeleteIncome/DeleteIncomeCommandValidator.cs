using FluentValidation;

namespace Expensify.Modules.Income.Application.Incomes.Command.DeleteIncome;

internal sealed class DeleteIncomeCommandValidator : AbstractValidator<DeleteIncomeCommand>
{
    public DeleteIncomeCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.IncomeId).NotEmpty();
    }
}
