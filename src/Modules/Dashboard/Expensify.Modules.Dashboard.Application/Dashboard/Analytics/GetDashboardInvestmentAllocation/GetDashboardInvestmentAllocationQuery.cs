using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentAllocation;

public sealed record GetDashboardInvestmentAllocationQuery(Guid UserId) : IQuery<DashboardInvestmentAllocationResponse>;
