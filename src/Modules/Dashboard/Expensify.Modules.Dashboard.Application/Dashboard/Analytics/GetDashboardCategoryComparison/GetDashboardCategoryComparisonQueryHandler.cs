using System.Data.Common;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCategoryComparison;

internal sealed class GetDashboardCategoryComparisonQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardCategoryComparisonQuery, DashboardCategoryComparisonResponse>
{
    public async Task<Result<DashboardCategoryComparisonResponse>> Handle(
        GetDashboardCategoryComparisonQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        Result<DashboardUserSettings> userSettingsResult =
            await DashboardReadModelQueries.GetUserSettingsAsync(connection, request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<DashboardCategoryComparisonResponse>(userSettingsResult.Error);
        }

        DashboardUserSettings userSettings = userSettingsResult.Value;
        var fallbackPeriod = DashboardPeriod.CreateCurrent(dateTimeProvider.UtcNow, userSettings.Timezone, userSettings.MonthStartDay);
        DashboardPeriod currentPeriod = DashboardPeriod.TryCreate(request.Month, userSettings.MonthStartDay, out DashboardPeriod? explicitPeriod)
            ? explicitPeriod!
            : fallbackPeriod;
        DashboardPeriod previousPeriod = currentPeriod.Previous();

        List<DashboardExpenseRow> expenses = await DashboardReadModelQueries.GetExpensesAsync(
            connection,
            request.UserId,
            previousPeriod.StartDate,
            currentPeriod.EndDateExclusive,
            cancellationToken);

        var currentTotals = expenses
            .Where(row => currentPeriod.Contains(row.TransactionDate))
            .GroupBy(row => row.Category, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount), StringComparer.Ordinal);

        var previousTotals = expenses
            .Where(row => previousPeriod.Contains(row.TransactionDate))
            .GroupBy(row => row.Category, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount), StringComparer.Ordinal);

        IReadOnlyCollection<DashboardCategoryComparisonItemResponse> categories = currentTotals.Keys
            .Union(previousTotals.Keys, StringComparer.Ordinal)
            .Select(category =>
            {
                decimal currentAmount = currentTotals.GetValueOrDefault(category);
                decimal previousAmount = previousTotals.GetValueOrDefault(category);
                decimal changeAmount = currentAmount - previousAmount;

                return new DashboardCategoryComparisonItemResponse(
                    category,
                    currentAmount,
                    previousAmount,
                    changeAmount,
                    DashboardCalculations.CalculateChangePercentage(currentAmount, previousAmount));
            })
            .OrderByDescending(item => item.CurrentAmount)
            .ThenByDescending(item => item.PreviousAmount)
            .ThenBy(item => item.Category, StringComparer.Ordinal)
            .ToList();

        return new DashboardCategoryComparisonResponse(
            currentPeriod.DisplayLabel,
            previousPeriod.DisplayLabel,
            userSettings.Currency,
            categories);
    }
}
