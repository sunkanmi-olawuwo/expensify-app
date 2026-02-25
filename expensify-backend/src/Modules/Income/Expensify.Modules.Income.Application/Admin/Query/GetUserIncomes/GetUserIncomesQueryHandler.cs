using MediatR;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Incomes.Query.GetIncomes;

namespace Expensify.Modules.Income.Application.Admin.Query.GetUserIncomes;

internal sealed class GetUserIncomesQueryHandler(IMediator mediator)
    : IQueryHandler<GetUserIncomesQuery, IncomePageResponse>
{
    public async Task<Result<IncomePageResponse>> Handle(GetUserIncomesQuery request, CancellationToken cancellationToken)
    {
        return await mediator.Send(
            new GetIncomesQuery(
                request.UserId,
                request.Period,
                request.Source,
                request.Type,
                request.MinAmount,
                request.MaxAmount,
                request.SortBy,
                request.SortOrder,
                request.Page,
                request.PageSize),
            cancellationToken);
    }
}
