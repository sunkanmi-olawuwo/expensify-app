using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategory;

internal sealed class GetExpenseCategoryQueryHandler(IExpenseCategoryRepository categoryRepository)
    : IQueryHandler<GetExpenseCategoryQuery, ExpenseCategoryResponse>
{
    public async Task<Result<ExpenseCategoryResponse>> Handle(GetExpenseCategoryQuery request, CancellationToken cancellationToken)
    {
        ExpenseCategory? category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.UserId != request.UserId)
        {
            return Result.Failure<ExpenseCategoryResponse>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        return new ExpenseCategoryResponse(category.Id, category.UserId, category.Name);
    }
}
