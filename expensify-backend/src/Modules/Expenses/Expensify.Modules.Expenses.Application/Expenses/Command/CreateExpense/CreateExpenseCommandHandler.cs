using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.Users;
using Expensify.Modules.Expenses.Domain.Categories;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Application.Expenses.Command.CreateExpense;

internal sealed class CreateExpenseCommandHandler(
    IExpenseRepository expenseRepository,
    IExpenseCategoryRepository categoryRepository,
    IExpenseTagRepository tagRepository,
    IUserSettingsService userSettingsService,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<CreateExpenseCommand, ExpenseResponse>
{
    public async Task<Result<ExpenseResponse>> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<ExpenseResponse>(userSettingsResult.Error);
        }

        ExpenseCategory? category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || category.UserId != request.UserId)
        {
            return Result.Failure<ExpenseResponse>(ExpenseErrors.CategoryNotFound(request.CategoryId));
        }

        IReadOnlyCollection<ExpenseTag> tags = await tagRepository.GetByIdsAsync(request.UserId, request.TagIds, cancellationToken);

        if (tags.Count != request.TagIds.Distinct().Count())
        {
            Guid missingTagId = request.TagIds.Except(tags.Select(t => t.Id)).First();
            return Result.Failure<ExpenseResponse>(ExpenseErrors.TagNotFound(missingTagId));
        }

        Result<Expense> expenseResult = Expense.Create(
            request.UserId,
            new Money(request.Amount, request.Currency),
            new ExpenseDate(request.Date),
            request.CategoryId,
            request.Merchant,
            request.Note,
            request.PaymentMethod,
            tags,
            userSettingsResult.Value.Currency);

        if (expenseResult.IsFailure)
        {
            return Result.Failure<ExpenseResponse>(expenseResult.Error);
        }

        expenseRepository.Add(expenseResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExpenseResponse(
            expenseResult.Value.Id,
            expenseResult.Value.UserId,
            expenseResult.Value.Amount,
            expenseResult.Value.Currency,
            expenseResult.Value.ExpenseDate,
            category.Id,
            category.Name,
            expenseResult.Value.Merchant,
            expenseResult.Value.Note,
            expenseResult.Value.PaymentMethod.ToString(),
            tags.Select(t => t.Id).ToList(),
            tags.Select(t => t.Name).ToList());
    }
}
