using FluentValidation;

namespace Expensify.Modules.Income.Application.Incomes.Command.CreateIncome;

internal sealed class CreateIncomeCommandValidator : AbstractValidator<CreateIncomeCommand>
{
    public CreateIncomeCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Currency).Matches("^[A-Z]{3}$");
        RuleFor(c => c.Source).MaximumLength(150);
        RuleFor(c => c.Note).MaximumLength(1000);
    }
}
