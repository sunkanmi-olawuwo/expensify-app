using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;
using Expensify.Modules.Expenses.Domain.Tags;

namespace Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTag;

internal sealed class GetExpenseTagQueryHandler(IExpenseTagRepository tagRepository)
    : IQueryHandler<GetExpenseTagQuery, ExpenseTagResponse>
{
    public async Task<Result<ExpenseTagResponse>> Handle(GetExpenseTagQuery request, CancellationToken cancellationToken)
    {
        ExpenseTag? tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag is null || tag.UserId != request.UserId)
        {
            return Result.Failure<ExpenseTagResponse>(ExpenseErrors.TagNotFound(request.TagId));
        }

        return new ExpenseTagResponse(tag.Id, tag.UserId, tag.Name);
    }
}
