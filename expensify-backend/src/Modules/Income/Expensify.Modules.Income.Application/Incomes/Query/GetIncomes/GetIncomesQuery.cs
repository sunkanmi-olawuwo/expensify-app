using Expensify.Common.Application.Messaging;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Domain.Incomes;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetIncomes;

public sealed record GetIncomesQuery(
    Guid UserId,
    string Period,
    string Source = "",
    IncomeType? Type = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string SortBy = "date",
    string SortOrder = "desc",
    int Page = 1,
    int PageSize = 20) : IQuery<IncomePageResponse>;
