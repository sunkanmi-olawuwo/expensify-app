using FluentValidation;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetDeletedExpenses;

internal sealed class GetDeletedExpensesQueryValidator : AbstractValidator<GetDeletedExpensesQuery>
{
    public GetDeletedExpensesQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).GreaterThan(0);
    }
}