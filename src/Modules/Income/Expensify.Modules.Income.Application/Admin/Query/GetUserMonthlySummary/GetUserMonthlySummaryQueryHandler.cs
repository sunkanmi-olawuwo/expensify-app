using MediatR;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Incomes.Query.GetMonthlySummary;

namespace Expensify.Modules.Income.Application.Admin.Query.GetUserMonthlySummary;

internal sealed class GetUserMonthlySummaryQueryHandler(IMediator mediator)
    : IQueryHandler<GetUserMonthlySummaryQuery, MonthlyIncomeSummaryResponse>
{
    public async Task<Result<MonthlyIncomeSummaryResponse>> Handle(GetUserMonthlySummaryQuery request, CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetMonthlySummaryQuery(request.UserId, request.Period), cancellationToken);
    }
}
