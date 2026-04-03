using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentTrend;

public sealed record GetDashboardInvestmentTrendQuery(Guid UserId, int Months) : IQuery<DashboardInvestmentTrendResponse>;
