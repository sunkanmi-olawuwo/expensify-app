using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.RestoreExpense;

internal sealed class RestoreExpenseCommandHandler(
    IExpenseRepository expenseRepository,
    IExpensesUnitOfWork unitOfWork) : ICommandHandler<RestoreExpenseCommand>
{
    public async Task<Result> Handle(RestoreExpenseCommand request, CancellationToken cancellationToken)
    {
        Expense? expense = await expenseRepository.GetByIdIncludingDeletedAsync(request.ExpenseId, cancellationToken);
        if (expense is null || expense.UserId != request.UserId)
        {
            return Result.Failure(ExpenseErrors.NotFound(request.ExpenseId));
        }

        Result restoreResult = expense.Restore();
        if (restoreResult.IsFailure)
        {
            return Result.Failure(ExpenseErrors.NotFound(request.ExpenseId));
        }

        expenseRepository.Update(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
