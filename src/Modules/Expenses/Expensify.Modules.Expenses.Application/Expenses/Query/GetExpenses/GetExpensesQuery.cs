using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetExpenses;

public sealed record GetExpensesQuery(
    Guid UserId,
    string Period,
    Guid? CategoryId = null,
    string Merchant = "",
    Guid[]? TagIds = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string PaymentMethod = "",
    string SortBy = "date",
    string SortOrder = "desc",
    int Page = 1,
    int PageSize = 20) : IQuery<ExpensesPageResponse>;
