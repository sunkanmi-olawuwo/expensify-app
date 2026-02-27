using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.Users;
using Expensify.Modules.Income.Application.Incomes;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetMonthlySummary;

internal sealed class GetMonthlySummaryQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserSettingsService userSettingsService)
    : IQueryHandler<GetMonthlySummaryQuery, MonthlyIncomeSummaryResponse>
{
    public async Task<Result<MonthlyIncomeSummaryResponse>> Handle(GetMonthlySummaryQuery request, CancellationToken cancellationToken)
    {
        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<MonthlyIncomeSummaryResponse>(userSettingsResult.Error);
        }

        Result<MonthPeriod> periodResult = MonthPeriod.Create(request.Period, userSettingsResult.Value.MonthStartDay);
        if (periodResult.IsFailure)
        {
            return Result.Failure<MonthlyIncomeSummaryResponse>(periodResult.Error);
        }

        var periodStart = periodResult.Value.StartDate.ToDateTime(TimeOnly.MinValue);
        var periodEndExclusive = periodResult.Value.EndDateExclusive.ToDateTime(TimeOnly.MinValue);

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string totalSql =
            """
            SELECT COALESCE(SUM(i.amount), 0) AS TotalAmount, COUNT(*) AS IncomeCount
            FROM income.incomes i
            WHERE i.user_id = @UserId
              AND i.deleted_at_utc IS NULL
              AND i.income_date >= @StartDate
              AND i.income_date < @EndDateExclusive
            """;

        SummaryRow total = await connection.QuerySingleAsync<SummaryRow>(totalSql, new
        {
            request.UserId,
            StartDate = periodStart,
            EndDateExclusive = periodEndExclusive
        });

        const string typesSql =
            """
            SELECT i.type AS Type, SUM(i.amount) AS Amount
            FROM income.incomes i
            WHERE i.user_id = @UserId
              AND i.deleted_at_utc IS NULL
              AND i.income_date >= @StartDate
              AND i.income_date < @EndDateExclusive
            GROUP BY i.type
            ORDER BY amount DESC, i.type
            """;

        List<TypeTotalRow> typeRows = (await connection.QueryAsync<TypeTotalRow>(typesSql, new
        {
            request.UserId,
            StartDate = periodStart,
            EndDateExclusive = periodEndExclusive
        })).AsList();

        IReadOnlyCollection<IncomeTypeTotalResponse> types = typeRows
            .Select(row => new IncomeTypeTotalResponse(row.Type, row.Amount))
            .ToList();

        return new MonthlyIncomeSummaryResponse(periodResult.Value.Period, total.TotalAmount, total.IncomeCount, types);
    }

    private sealed class SummaryRow
    {
        public decimal TotalAmount { get; init; } = 0m;
        public int IncomeCount { get; init; } = -1;
    }

    private sealed class TypeTotalRow
    {
        public string Type { get; init; } = string.Empty;
        public decimal Amount { get; init; } = 0m;
    }
}
