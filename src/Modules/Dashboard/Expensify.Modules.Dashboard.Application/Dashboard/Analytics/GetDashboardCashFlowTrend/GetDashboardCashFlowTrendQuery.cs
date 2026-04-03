using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCashFlowTrend;

public sealed record GetDashboardCashFlowTrendQuery(Guid UserId, int Months) : IQuery<DashboardCashFlowTrendResponse>;
