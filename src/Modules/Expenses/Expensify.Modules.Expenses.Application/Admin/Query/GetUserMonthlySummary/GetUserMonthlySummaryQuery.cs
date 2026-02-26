using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Admin.Query.GetUserMonthlySummary;

public sealed record GetUserMonthlySummaryQuery(Guid UserId, string Period) : IQuery<MonthlyExpensesSummaryResponse>;
