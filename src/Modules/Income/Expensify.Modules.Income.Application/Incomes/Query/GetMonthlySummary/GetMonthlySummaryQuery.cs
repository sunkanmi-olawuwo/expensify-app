using Expensify.Common.Application.Messaging;
using Expensify.Modules.Income.Application.Abstractions;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetMonthlySummary;

public sealed record GetMonthlySummaryQuery(Guid UserId, string Period) : IQuery<MonthlyIncomeSummaryResponse>;
