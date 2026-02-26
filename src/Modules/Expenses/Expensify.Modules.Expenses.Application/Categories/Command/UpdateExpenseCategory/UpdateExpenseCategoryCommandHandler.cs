using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Categories.Command.UpdateExpenseCategory;

internal sealed class UpdateExpenseCategoryCommandHandler(
    IExpenseCategoryRepository categoryRepository,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<UpdateExpenseCategoryCommand, ExpenseCategoryResponse>
{
    public async Task<Result<ExpenseCategoryResponse>> Handle(UpdateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        ExpenseCategory? category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.UserId != request.UserId)
        {
            return Result.Failure<ExpenseCategoryResponse>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        bool exists = await categoryRepository.ExistsByNameAsync(request.UserId, request.Name, request.CategoryId, cancellationToken);
        if (exists)
        {
            return Result.Failure<ExpenseCategoryResponse>(
                Error.Conflict("Expenses.CategoryAlreadyExists", $"Category '{request.Name}' already exists"));
        }

        category.Update(request.Name);
        categoryRepository.Update(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExpenseCategoryResponse(category.Id, category.UserId, category.Name);
    }
}
