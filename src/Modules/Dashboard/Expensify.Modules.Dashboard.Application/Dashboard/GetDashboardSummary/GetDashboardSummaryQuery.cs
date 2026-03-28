using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Dashboard.Application.Dashboard.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery(Guid UserId) : IQuery<DashboardSummaryResponse>;
