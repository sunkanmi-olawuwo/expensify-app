using Expensify.Common.Application.Messaging;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Domain.Incomes;

namespace Expensify.Modules.Income.Application.Admin.Query.GetUserIncomes;

public sealed record GetUserIncomesQuery(
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
