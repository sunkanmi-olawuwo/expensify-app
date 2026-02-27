using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.Users;
using Expensify.Modules.Expenses.Application.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetMonthlySummary;

internal sealed class GetMonthlySummaryQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserSettingsService userSettingsService)
    : IQueryHandler<GetMonthlySummaryQuery, MonthlyExpensesSummaryResponse>
{
    public async Task<Result<MonthlyExpensesSummaryResponse>> Handle(GetMonthlySummaryQuery request, CancellationToken cancellationToken)
    {
        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<MonthlyExpensesSummaryResponse>(userSettingsResult.Error);
        }

        Result<MonthPeriod> periodResult = MonthPeriod.Create(request.Period, userSettingsResult.Value.MonthStartDay);
        if (periodResult.IsFailure)
        {
            return Result.Failure<MonthlyExpensesSummaryResponse>(periodResult.Error);
        }
        var periodStart = periodResult.Value.StartDate.ToDateTime(TimeOnly.MinValue);
        var periodEndExclusive = periodResult.Value.EndDateExclusive.ToDateTime(TimeOnly.MinValue);

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string totalSql =
            """
            SELECT COALESCE(SUM(e.amount), 0) AS TotalAmount, COUNT(*) AS ExpenseCount
            FROM expenses.expenses e
            WHERE e.user_id = @UserId
              AND e.deleted_at_utc IS NULL
              AND e.expense_date >= @StartDate
              AND e.expense_date < @EndDateExclusive
            """;

        SummaryRow total = await connection.QuerySingleAsync<SummaryRow>(totalSql, new
        {
            request.UserId,
            StartDate = periodStart,
            EndDateExclusive = periodEndExclusive
        });

        const string categoriesSql =
            """
            SELECT e.category_id AS CategoryId, c.name AS CategoryName, SUM(e.amount) AS Amount
            FROM expenses.expenses e
            INNER JOIN expenses.expense_categories c ON c.id = e.category_id
            WHERE e.user_id = @UserId
              AND e.deleted_at_utc IS NULL
              AND e.expense_date >= @StartDate
              AND e.expense_date < @EndDateExclusive
            GROUP BY e.category_id, c.name
            ORDER BY amount DESC, c.name
            """;

        List<CategoryTotalRow> categoryRows = (await connection.QueryAsync<CategoryTotalRow>(categoriesSql, new
        {
            request.UserId,
            StartDate = periodStart,
            EndDateExclusive = periodEndExclusive
        })).AsList();

        IReadOnlyCollection<CategoryTotalResponse> categories = categoryRows
            .Select(row => new CategoryTotalResponse(row.CategoryId, row.CategoryName, row.Amount))
            .ToList();

        return new MonthlyExpensesSummaryResponse(periodResult.Value.Period, total.TotalAmount, total.ExpenseCount, categories);
    }

    private sealed class SummaryRow
    {
        public decimal TotalAmount { get; init; } = 0m;
        public int ExpenseCount { get; init; } = -1;
    }

    private sealed class CategoryTotalRow
    {
        public Guid CategoryId { get; init; } = Guid.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public decimal Amount { get; init; } = 0m;
    }
}
