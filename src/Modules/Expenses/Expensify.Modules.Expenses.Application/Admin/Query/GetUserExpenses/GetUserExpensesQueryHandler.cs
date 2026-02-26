using MediatR;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Query.GetExpenses;

namespace Expensify.Modules.Expenses.Application.Admin.Query.GetUserExpenses;

internal sealed class GetUserExpensesQueryHandler(IMediator mediator)
    : IQueryHandler<GetUserExpensesQuery, ExpensesPageResponse>
{
    public async Task<Result<ExpensesPageResponse>> Handle(GetUserExpensesQuery request, CancellationToken cancellationToken)
    {
        return await mediator.Send(
            new GetExpensesQuery(
                request.UserId,
                request.Period,
                request.CategoryId,
                request.Merchant,
                request.TagIds,
                request.MinAmount,
                request.MaxAmount,
                request.PaymentMethod,
                request.SortBy,
                request.SortOrder,
                request.Page,
                request.PageSize),
            cancellationToken);
    }
}
