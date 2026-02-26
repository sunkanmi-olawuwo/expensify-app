using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Application.Tags.Command.DeleteExpenseTag;

internal sealed class DeleteExpenseTagCommandHandler(
    IExpenseTagRepository tagRepository,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<DeleteExpenseTagCommand>
{
    public async Task<Result> Handle(DeleteExpenseTagCommand request, CancellationToken cancellationToken)
    {
        ExpenseTag? tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag is null || tag.UserId != request.UserId)
        {
            return Result.Failure(ExpenseErrors.TagNotFound(request.TagId));
        }

        tag.RaiseDeletedEvent();
        tagRepository.Remove(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
