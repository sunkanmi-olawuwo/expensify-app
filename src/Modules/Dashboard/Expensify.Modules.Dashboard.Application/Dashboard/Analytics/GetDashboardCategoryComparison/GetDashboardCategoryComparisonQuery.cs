using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCategoryComparison;

public sealed record GetDashboardCategoryComparisonQuery(Guid UserId, string? Month) : IQuery<DashboardCategoryComparisonResponse>;
