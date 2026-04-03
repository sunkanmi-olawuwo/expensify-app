using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardIncomeBreakdown;

public sealed record GetDashboardIncomeBreakdownQuery(Guid UserId, int Months) : IQuery<DashboardIncomeBreakdownResponse>;
