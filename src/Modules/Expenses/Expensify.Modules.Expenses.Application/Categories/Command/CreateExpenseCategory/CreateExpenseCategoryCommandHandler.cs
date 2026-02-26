using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Categories;

namespace Expensify.Modules.Expenses.Application.Categories.Command.CreateExpenseCategory;

internal sealed class CreateExpenseCategoryCommandHandler(
    IExpenseCategoryRepository categoryRepository,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<CreateExpenseCategoryCommand, ExpenseCategoryResponse>
{
    public async Task<Result<ExpenseCategoryResponse>> Handle(CreateExpenseCategoryCommand request, CancellationToken cancellationToken)
    {
        bool exists = await categoryRepository.ExistsByNameAsync(request.UserId, request.Name, null, cancellationToken);
        if (exists)
        {
            return Result.Failure<ExpenseCategoryResponse>(
                Error.Conflict("Expenses.CategoryAlreadyExists", $"Category '{request.Name}' already exists"));
        }

        var category = ExpenseCategory.Create(request.UserId, request.Name);
        categoryRepository.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExpenseCategoryResponse(category.Id, category.UserId, category.Name);
    }
}
