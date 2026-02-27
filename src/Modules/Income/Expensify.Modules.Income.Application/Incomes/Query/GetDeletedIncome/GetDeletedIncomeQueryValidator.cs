using FluentValidation;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetDeletedIncome;

internal sealed class GetDeletedIncomeQueryValidator : AbstractValidator<GetDeletedIncomeQuery>
{
    public GetDeletedIncomeQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).GreaterThan(0);
    }
}