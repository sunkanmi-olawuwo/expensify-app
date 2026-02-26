using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.Users;
using Expensify.Modules.Income.Application.Incomes;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetIncomes;

internal sealed class GetIncomesQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserSettingsService userSettingsService)
    : IQueryHandler<GetIncomesQuery, IncomePageResponse>
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<IncomePageResponse>> Handle(GetIncomesQuery request, CancellationToken cancellationToken)
    {
        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<IncomePageResponse>(userSettingsResult.Error);
        }

        Result<MonthPeriod> periodResult = MonthPeriod.Create(request.Period, userSettingsResult.Value.MonthStartDay);
        if (periodResult.IsFailure)
        {
            return Result.Failure<IncomePageResponse>(periodResult.Error);
        }

        int page = request.Page > 0 ? request.Page : DefaultPage;
        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, MaxPageSize) : DefaultPageSize;
        int offset = (page - 1) * pageSize;
        var periodStart = periodResult.Value.StartDate.ToDateTime(TimeOnly.MinValue);
        var periodEndExclusive = periodResult.Value.EndDateExclusive.ToDateTime(TimeOnly.MinValue);

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        string orderByColumn = ResolveSortColumn(request.SortBy);
        string orderDirection = ResolveSortOrder(request.SortOrder);
        string sourceSearch = string.IsNullOrWhiteSpace(request.Source)
            ? string.Empty
            : $"%{request.Source.Trim()}%";
        string type = request.Type?.ToString() ?? string.Empty;

        const string filteredIncomeCte =
            """
            WITH filtered_income AS (
                SELECT
                    i.id,
                    i.amount,
                    i.currency,
                    i.income_date AS date,
                    i.source,
                    i.type,
                    i.note
                FROM income.incomes i
                WHERE i.user_id = @UserId
                  AND i.income_date >= @StartDate
                  AND i.income_date < @EndDateExclusive
                  AND (@Source = '' OR i.source ILIKE @Source)
                  AND (@Type = '' OR i.type = @Type)
                  AND (@MinAmount::numeric IS NULL OR i.amount >= @MinAmount::numeric)
                  AND (@MaxAmount::numeric IS NULL OR i.amount <= @MaxAmount::numeric)
            )
            """;

        string countSql =
            $"""
            {filteredIncomeCte}
            SELECT COUNT(*)
            FROM filtered_income
            """;

        string pageSql =
            $"""
            {filteredIncomeCte}
            SELECT
                id,
                amount,
                currency,
                date,
                source,
                type,
                note
            FROM filtered_income
            ORDER BY {orderByColumn} {orderDirection}
            LIMIT @PageSize OFFSET @Offset
            """;

        DynamicParameters parameters = new();
        parameters.Add("UserId", request.UserId);
        parameters.Add("StartDate", periodStart);
        parameters.Add("EndDateExclusive", periodEndExclusive);
        parameters.Add("Source", sourceSearch);
        parameters.Add("Type", type);
        parameters.Add("MinAmount", request.MinAmount);
        parameters.Add("MaxAmount", request.MaxAmount);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        List<IncomeRow> rows = (await connection.QueryAsync<IncomeRow>(pageSql, parameters)).AsList();

        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        IReadOnlyCollection<IncomeListItemResponse> items = rows.Select(row =>
            new IncomeListItemResponse(
                row.Id,
                row.Amount,
                row.Currency,
                row.Date,
                row.Source,
                row.Type,
                row.Note)).ToList();

        return new IncomePageResponse(page, pageSize, totalCount, page, totalPages, items);
    }

    private static string ResolveSortColumn(string sortBy) =>
        sortBy.Trim().ToLowerInvariant() switch
        {
            "amount" => "amount",
            "source" => "source",
            _ => "date"
        };

    private static string ResolveSortOrder(string sortOrder) =>
        sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

    private sealed class IncomeRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public decimal Amount { get; init; } = 0m;
        public string Currency { get; init; } = string.Empty;
        public DateOnly Date { get; init; } = DateOnly.MinValue;
        public string Source { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
    }
}
