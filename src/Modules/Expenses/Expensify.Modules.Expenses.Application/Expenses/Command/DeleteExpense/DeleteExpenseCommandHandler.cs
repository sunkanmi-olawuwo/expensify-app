using Expensify.Common.Application.Data;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.DeleteExpense;

internal sealed class DeleteExpenseCommandHandler(
    IExpenseRepository expenseRepository,
    IDateTimeProvider dateTimeProvider,
    IExpensesUnitOfWork unitOfWork) : ICommandHandler<DeleteExpenseCommand>
{
    public async Task<Result> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        Expense? expense = await expenseRepository.GetByIdIncludingDeletedAsync(request.ExpenseId, cancellationToken);
        if (expense is null || expense.UserId != request.UserId)
        {
            return Result.Failure(ExpenseErrors.NotFound(request.ExpenseId));
        }

        Result markDeletedResult = expense.MarkDeleted(dateTimeProvider.UtcNow);
        if (markDeletedResult.IsFailure)
        {
            return Result.Failure(ExpenseErrors.NotFound(request.ExpenseId));
        }

        expenseRepository.Update(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
