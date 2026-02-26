using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Application.Tags.Command.CreateExpenseTag;

internal sealed class CreateExpenseTagCommandHandler(
    IExpenseTagRepository tagRepository,
    IExpensesUnitOfWork unitOfWork)
    : ICommandHandler<CreateExpenseTagCommand, ExpenseTagResponse>
{
    public async Task<Result<ExpenseTagResponse>> Handle(CreateExpenseTagCommand request, CancellationToken cancellationToken)
    {
        bool exists = await tagRepository.ExistsByNameAsync(request.UserId, request.Name, null, cancellationToken);
        if (exists)
        {
            return Result.Failure<ExpenseTagResponse>(
                Error.Conflict("Expenses.TagAlreadyExists", $"Tag '{request.Name}' already exists"));
        }

        var tag = ExpenseTag.Create(request.UserId, request.Name);
        tagRepository.Add(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExpenseTagResponse(tag.Id, tag.UserId, tag.Name);
    }
}
