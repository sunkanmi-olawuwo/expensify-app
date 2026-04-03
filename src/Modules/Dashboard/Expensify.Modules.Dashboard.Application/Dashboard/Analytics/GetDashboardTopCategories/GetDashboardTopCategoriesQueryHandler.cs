using System.Data.Common;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardTopCategories;

internal sealed class GetDashboardTopCategoriesQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardTopCategoriesQuery, DashboardTopCategoriesResponse>
{
    public async Task<Result<DashboardTopCategoriesResponse>> Handle(
        GetDashboardTopCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        Result<DashboardUserSettings> userSettingsResult =
            await DashboardReadModelQueries.GetUserSettingsAsync(connection, request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<DashboardTopCategoriesResponse>(userSettingsResult.Error);
        }

        DashboardUserSettings userSettings = userSettingsResult.Value;
        int months = DashboardCalculations.NormalizeLookbackMonths(request.Months, 3);
        int limit = DashboardCalculations.NormalizeTopCategoryLimit(request.Limit, 5);
        var currentPeriod = DashboardPeriod.CreateCurrent(dateTimeProvider.UtcNow, userSettings.Timezone, userSettings.MonthStartDay);
        List<DashboardPeriod> periods = DashboardPeriod.CreateHistory(currentPeriod, months);

        List<DashboardExpenseRow> expenses = await DashboardReadModelQueries.GetExpensesAsync(
            connection,
            request.UserId,
            periods[0].StartDate,
            currentPeriod.EndDateExclusive,
            cancellationToken);

        var rankedCategories = expenses
            .GroupBy(row => row.Category, StringComparer.Ordinal)
            .Select(group => new CategorySpend(group.Key, group.Sum(item => item.Amount)))
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.Category, StringComparer.Ordinal)
            .ToList();

        decimal totalSpent = rankedCategories.Sum(item => item.Amount);
        var topCategories = rankedCategories.Take(limit).ToList();
        IReadOnlyList<decimal> percentages = DashboardCalculations.CalculatePercentages(
            topCategories.Select(item => item.Amount).ToList(),
            totalSpent,
            correctFinalPercentage: topCategories.Count == rankedCategories.Count);

        IReadOnlyCollection<DashboardTopCategoryResponse> categories = topCategories
            .Select((category, index) => new DashboardTopCategoryResponse(
                index + 1,
                category.Category,
                category.Amount,
                percentages[index],
                DashboardCalculations.GetColorKey(index)))
            .ToList();

        return new DashboardTopCategoriesResponse(
            DashboardCalculations.FormatLookbackPeriod(months),
            userSettings.Currency,
            totalSpent,
            categories);
    }

    private sealed record CategorySpend(string Category, decimal Amount);
}
