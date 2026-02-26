using Expensify.Common.Application.Messaging;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetMonthlySummary;

public sealed record GetMonthlySummaryQuery(Guid UserId, string Period) : IQuery<MonthlyExpensesSummaryResponse>;
