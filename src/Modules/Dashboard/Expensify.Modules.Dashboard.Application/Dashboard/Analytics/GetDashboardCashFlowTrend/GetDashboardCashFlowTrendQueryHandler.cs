using System.Data.Common;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Dashboard.Application.Dashboard;

namespace Expensify.Modules.Dashboard.Application.Dashboard.Analytics.GetDashboardCashFlowTrend;

internal sealed class GetDashboardCashFlowTrendQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardCashFlowTrendQuery, DashboardCashFlowTrendResponse>
{
    public async Task<Result<DashboardCashFlowTrendResponse>> Handle(
        GetDashboardCashFlowTrendQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        Result<DashboardUserSettings> userSettingsResult =
            await DashboardReadModelQueries.GetUserSettingsAsync(connection, request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<DashboardCashFlowTrendResponse>(userSettingsResult.Error);
        }

        DashboardUserSettings userSettings = userSettingsResult.Value;
        int months = DashboardCalculations.NormalizeTrendMonths(request.Months, 6);
        var currentPeriod = DashboardPeriod.CreateCurrent(dateTimeProvider.UtcNow, userSettings.Timezone, userSettings.MonthStartDay);
        List<DashboardPeriod> periods = DashboardPeriod.CreateHistory(currentPeriod, months);

        List<DashboardExpenseRow> expenses = await DashboardReadModelQueries.GetExpensesAsync(
            connection,
            request.UserId,
            periods[0].StartDate,
            currentPeriod.EndDateExclusive,
            cancellationToken);

        List<DashboardIncomeRow> incomes = await DashboardReadModelQueries.GetIncomesAsync(
            connection,
            request.UserId,
            periods[0].StartDate,
            currentPeriod.EndDateExclusive,
            cancellationToken);

        IReadOnlyCollection<DashboardCashFlowTrendMonthResponse> responseMonths = periods
            .Select(period =>
            {
                decimal income = DashboardCalculations.SumInPeriod(incomes, period);
                decimal expensesTotal = DashboardCalculations.SumInPeriod(expenses, period);
                decimal netCashFlow = income - expensesTotal;

                return new DashboardCashFlowTrendMonthResponse(
                    period.DisplayLabel,
                    income,
                    expensesTotal,
                    netCashFlow,
                    DashboardCalculations.CalculateSavingsRate(income, expensesTotal));
            })
            .ToList();

        return new DashboardCashFlowTrendResponse(responseMonths, userSettings.Currency);
    }
}
