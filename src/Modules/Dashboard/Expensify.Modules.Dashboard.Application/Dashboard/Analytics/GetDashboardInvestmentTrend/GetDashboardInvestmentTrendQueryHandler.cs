using System.Data.Common;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardInvestmentTrend;

internal sealed class GetDashboardInvestmentTrendQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardInvestmentTrendQuery, DashboardInvestmentTrendResponse>
{
    public async Task<Result<DashboardInvestmentTrendResponse>> Handle(
        GetDashboardInvestmentTrendQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        Result<DashboardUserSettings> userSettingsResult =
            await DashboardReadModelQueries.GetUserSettingsAsync(connection, request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<DashboardInvestmentTrendResponse>(userSettingsResult.Error);
        }

        DashboardUserSettings userSettings = userSettingsResult.Value;
        TimeZoneInfo timeZone = DashboardTimeZoneResolver.Resolve(userSettings.Timezone);
        int months = DashboardCalculations.NormalizeTrendMonths(request.Months, 6);
        var currentPeriod = DashboardPeriod.CreateCurrent(dateTimeProvider.UtcNow, userSettings.Timezone, userSettings.MonthStartDay);
        List<DashboardPeriod> periods = DashboardPeriod.CreateHistory(currentPeriod, months);

        List<DashboardInvestmentContributionRow> contributions = await DashboardReadModelQueries.GetInvestmentContributionsAsync(
            connection,
            request.UserId,
            periods[0].GetStartDateUtc(timeZone),
            currentPeriod.GetEndDateExclusiveUtc(timeZone),
            cancellationToken);

        IReadOnlyCollection<DashboardInvestmentTrendMonthResponse> responseMonths = periods
            .Select(period =>
            {
                var periodRows = contributions
                    .Where(row => period.Contains(DashboardCalculations.ToLocalDate(row.ContributionDate, timeZone)))
                    .ToList();

                return new DashboardInvestmentTrendMonthResponse(
                    period.DisplayLabel,
                    periodRows.Sum(row => row.Amount),
                    periodRows.Select(row => row.InvestmentId).Distinct().Count());
            })
            .ToList();

        return new DashboardInvestmentTrendResponse(
            userSettings.Currency,
            responseMonths.Sum(item => item.Contributions),
            responseMonths);
    }
}
