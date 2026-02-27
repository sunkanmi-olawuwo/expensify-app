using Expensify.Common.Application.Messaging;
using Expensify.Modules.Income.Application.Abstractions;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetDeletedIncome;

public sealed record GetDeletedIncomeQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IQuery<DeletedIncomePageResponse>;