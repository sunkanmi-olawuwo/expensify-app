using System.Data.Common;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentAllocation;

internal sealed class GetDashboardInvestmentAllocationQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetDashboardInvestmentAllocationQuery, DashboardInvestmentAllocationResponse>
{
    public async Task<Result<DashboardInvestmentAllocationResponse>> Handle(
        GetDashboardInvestmentAllocationQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        Result<DashboardUserSettings> userSettingsResult =
            await DashboardReadModelQueries.GetUserSettingsAsync(connection, request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<DashboardInvestmentAllocationResponse>(userSettingsResult.Error);
        }

        DashboardUserSettings userSettings = userSettingsResult.Value;
        List<DashboardInvestmentAllocationRow> categories = await DashboardReadModelQueries.GetInvestmentAllocationAsync(
            connection,
            request.UserId,
            cancellationToken);

        decimal totalValue = categories.Sum(item => item.TotalBalance);
        int accountCount = categories.Sum(item => item.AccountCount);
        IReadOnlyList<decimal> percentages = DashboardCalculations.CalculatePercentages(
            categories.Select(item => item.TotalBalance).ToList(),
            totalValue,
            correctFinalPercentage: true);

        IReadOnlyCollection<DashboardInvestmentAllocationCategoryResponse> responseCategories = categories
            .Select((category, index) => new DashboardInvestmentAllocationCategoryResponse(
                category.CategoryName,
                category.CategorySlug,
                category.TotalBalance,
                category.AccountCount,
                percentages[index],
                DashboardCalculations.GetColorKey(index)))
            .ToList();

        return new DashboardInvestmentAllocationResponse(
            userSettings.Currency,
            totalValue,
            accountCount,
            responseCategories);
    }
}
