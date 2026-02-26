using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Application.Tags.Command.UpdateExpenseTag;

internal sealed class UpdateExpenseTagCommandHandler(
    IExpenseTagRepository tagRepository,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<UpdateExpenseTagCommand, ExpenseTagResponse>
{
    public async Task<Result<ExpenseTagResponse>> Handle(UpdateExpenseTagCommand request, CancellationToken cancellationToken)
    {
        ExpenseTag? tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag is null || tag.UserId != request.UserId)
        {
            return Result.Failure<ExpenseTagResponse>(ExpenseErrors.TagNotFound(request.TagId));
        }

        bool exists = await tagRepository.ExistsByNameAsync(request.UserId, request.Name, request.TagId, cancellationToken);
        if (exists)
        {
            return Result.Failure<ExpenseTagResponse>(
                Error.Conflict("Expenses.TagAlreadyExists", $"Tag '{request.Name}' already exists"));
        }

        tag.Update(request.Name);
        tagRepository.Update(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExpenseTagResponse(tag.Id, tag.UserId, tag.Name);
    }
}
