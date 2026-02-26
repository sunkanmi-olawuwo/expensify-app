using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Categories.Command.DeleteExpenseCategory;

internal sealed class DeleteExpenseCategoryCommandHandler(
    IExpenseCategoryRepository categoryRepository,
    IExpenseRepository expenseRepository,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<DeleteExpenseCategoryCommand>
{
    public async Task<Result> Handle(DeleteExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        ExpenseCategory? category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.UserId != request.UserId)
        {
            return Result.Failure(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        bool categoryInUse = await expenseRepository.ExistsByCategoryAsync(request.UserId, request.CategoryId, cancellationToken);
        if (categoryInUse)
        {
            return Result.Failure(ExpenseErrors.CategoryInUse(request.CategoryId));
        }

        category.RaiseDeletedEvent();
        categoryRepository.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
