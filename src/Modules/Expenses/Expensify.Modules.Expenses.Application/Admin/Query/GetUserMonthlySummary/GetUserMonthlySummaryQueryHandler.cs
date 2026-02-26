using MediatR;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Expenses.Query.GetMonthlySummary;

namespace Expensify.Modules.Expenses.Application.Admin.Query.GetUserMonthlySummary;

internal sealed class GetUserMonthlySummaryQueryHandler(IMediator mediator)
    : IQueryHandler<GetUserMonthlySummaryQuery, MonthlyExpensesSummaryResponse>
{
    public async Task<Result<MonthlyExpensesSummaryResponse>> Handle(GetUserMonthlySummaryQuery request, CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetMonthlySummaryQuery(request.UserId, request.Period), cancellationToken);
    }
}
