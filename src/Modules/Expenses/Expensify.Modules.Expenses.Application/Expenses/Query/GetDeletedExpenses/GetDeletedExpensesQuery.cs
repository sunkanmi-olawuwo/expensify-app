using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetDeletedExpenses;

public sealed record GetDeletedExpensesQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IQuery<DeletedExpensesPageResponse>;