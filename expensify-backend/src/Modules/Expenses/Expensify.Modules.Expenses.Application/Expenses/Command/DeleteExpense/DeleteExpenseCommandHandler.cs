using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.DeleteExpense;

internal sealed class DeleteExpenseCommandHandler(
    IExpenseRepository expenseRepository,
    IExpensesUnitOfWork unitOfWork) : ICommandHandler<DeleteExpenseCommand>
{
    public async Task<Result> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        Expense? expense = await expenseRepository.GetByIdAsync(request.ExpenseId, cancellationToken);
        if (expense is null || expense.UserId != request.UserId)
        {
            return Result.Failure(ExpenseErrors.NotFound(request.ExpenseId));
        }

        expense.RaiseDeletedEvent();
        expenseRepository.Remove(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
