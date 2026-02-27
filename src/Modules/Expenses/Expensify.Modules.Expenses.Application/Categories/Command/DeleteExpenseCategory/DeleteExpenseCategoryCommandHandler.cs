using System.Reflection;
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

        try
        {
            category.RaiseDeletedEvent();
            categoryRepository.Remove(category);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (IsForeignKeyViolation(exception))
        {
            // Race condition fallback: the category became referenced between the pre-check and save.
            return Result.Failure(ExpenseErrors.CategoryInUse(request.CategoryId));
        }

        return Result.Success();
    }

    private static bool IsForeignKeyViolation(Exception exception)
    {
        const string ForeignKeyViolationSqlState = "23503";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

        Exception? current = exception;

        while (current is not null)
        {
            object? sqlState = current.GetType().GetProperty("SqlState", Flags)?.GetValue(current);
            sqlState ??= current.Data["SqlState"];
            if (sqlState is string sqlStateText &&
                sqlStateText.Equals(ForeignKeyViolationSqlState, StringComparison.Ordinal))
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
