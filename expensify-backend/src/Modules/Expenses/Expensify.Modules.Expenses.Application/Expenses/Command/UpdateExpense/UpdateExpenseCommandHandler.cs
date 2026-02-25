using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.Users;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.UpdateExpense;

internal sealed class UpdateExpenseCommandHandler(
    IExpenseRepository expenseRepository,
    IExpenseCategoryRepository categoryRepository,
    IExpenseTagRepository tagRepository,
    IUserSettingsService userSettingsService,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<UpdateExpenseCommand, ExpenseResponse>
{
    public async Task<Result<ExpenseResponse>> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        Expense? expense = await expenseRepository.GetByIdAsync(request.ExpenseId, cancellationToken);
        if (expense is null || expense.UserId != request.UserId)
        {
            return Result.Failure<ExpenseResponse>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        ExpenseCategory? category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.UserId != request.UserId)
        {
            return Result.Failure<ExpenseResponse>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<ExpenseResponse>(userSettingsResult.Error);
        }

        IReadOnlyCollection<ExpenseTag> tags = await tagRepository.GetByIdsAsync(request.UserId, request.TagIds, cancellationToken);
        if (tags.Count != request.TagIds.Distinct().Count())
        {
            Guid missingTagId = request.TagIds.Except(tags.Select(t => t.Id)).First();
            return Result.Failure<ExpenseResponse>(ExpenseErrors.TagNotFound(missingTagId));
        }

        Result updateResult = expense.Update(
            new Money(request.Amount, request.Currency),
            new ExpenseDate(request.Date),
            request.CategoryId,
            request.Merchant,
            request.Note,
            request.PaymentMethod,
            tags,
            userSettingsResult.Value.Currency);

        if (updateResult.IsFailure)
        {
            return Result.Failure<ExpenseResponse>(updateResult.Error);
        }

        expenseRepository.Update(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExpenseResponse(
            expense.Id,
            expense.UserId,
            expense.Amount,
            expense.Currency,
            expense.ExpenseDate,
            category.Id,
            category.Name,
            expense.Merchant,
            expense.Note,
            expense.PaymentMethod.ToString(),
            tags.Select(t => t.Id).ToList(),
            tags.Select(t => t.Name).ToList());
    }
}
