using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardTopCategories;

public sealed record GetDashboardTopCategoriesQuery(Guid UserId, int Months, int Limit) : IQuery<DashboardTopCategoriesResponse>;
