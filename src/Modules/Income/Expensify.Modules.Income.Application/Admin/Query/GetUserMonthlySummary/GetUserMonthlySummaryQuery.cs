using Expensify.Common.Application.Messaging;
using Expensify.Modules.Income.Application.Abstractions;

namespace Expensify.Modules.Income.Application.Admin.Query.GetUserMonthlySummary;

public sealed record GetUserMonthlySummaryQuery(Guid UserId, string Period) : IQuery<MonthlyIncomeSummaryResponse>;
