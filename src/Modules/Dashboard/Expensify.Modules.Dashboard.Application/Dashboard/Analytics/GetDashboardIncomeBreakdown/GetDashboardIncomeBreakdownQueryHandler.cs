using System.Data.Common;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardIncomeBreakdown;

internal sealed class GetDashboardIncomeBreakdownQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardIncomeBreakdownQuery, DashboardIncomeBreakdownResponse>
{
    public async Task<Result<DashboardIncomeBreakdownResponse>> Handle(
        GetDashboardIncomeBreakdownQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        Result<DashboardUserSettings> userSettingsResult =
            await DashboardReadModelQueries.GetUserSettingsAsync(connection, request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<DashboardIncomeBreakdownResponse>(userSettingsResult.Error);
        }

        DashboardUserSettings userSettings = userSettingsResult.Value;
        int months = DashboardCalculations.NormalizeLookbackMonths(request.Months, 3);
        var currentPeriod = DashboardPeriod.CreateCurrent(dateTimeProvider.UtcNow, userSettings.Timezone, userSettings.MonthStartDay);
        List<DashboardPeriod> periods = DashboardPeriod.CreateHistory(currentPeriod, months);

        List<DashboardIncomeRow> incomes = await DashboardReadModelQueries.GetIncomesAsync(
            connection,
            request.UserId,
            periods[0].StartDate,
            currentPeriod.EndDateExclusive,
            cancellationToken);

        var sources = incomes
            .GroupBy(row => row.Type, StringComparer.Ordinal)
            .Select(group => new IncomeAmount(group.Key, group.Sum(item => item.Amount)))
            .OrderByDescending(item => item.Amount)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .ToList();

        decimal totalIncome = sources.Sum(item => item.Amount);
        IReadOnlyList<decimal> percentages = DashboardCalculations.CalculatePercentages(
            sources.Select(item => item.Amount).ToList(),
            totalIncome,
            correctFinalPercentage: true);

        IReadOnlyCollection<DashboardIncomeBreakdownSourceResponse> responseSources = sources
            .Select((source, index) => new DashboardIncomeBreakdownSourceResponse(
                source.Source,
                source.Amount,
                percentages[index],
                DashboardCalculations.GetColorKey(index)))
            .ToList();

        return new DashboardIncomeBreakdownResponse(
            DashboardCalculations.FormatLookbackPeriod(months),
            userSettings.Currency,
            totalIncome,
            responseSources);
    }

    private sealed record IncomeAmount(string Source, decimal Amount);
}
