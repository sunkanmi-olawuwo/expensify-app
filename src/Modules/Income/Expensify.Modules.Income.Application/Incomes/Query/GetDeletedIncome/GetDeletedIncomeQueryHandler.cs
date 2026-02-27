using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Application.Abstractions.SoftDelete;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetDeletedIncome;

internal sealed class GetDeletedIncomeQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider,
    ISoftDeleteRetentionProvider softDeleteRetentionProvider) : IQueryHandler<GetDeletedIncomeQuery, DeletedIncomePageResponse>
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<DeletedIncomePageResponse>> Handle(GetDeletedIncomeQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page > 0 ? request.Page : DefaultPage;
        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, MaxPageSize) : DefaultPageSize;
        int offset = (page - 1) * pageSize;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

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
                    i.note,
                    i.deleted_at_utc
                FROM income.incomes i
                WHERE i.user_id = @UserId
                  AND i.deleted_at_utc IS NOT NULL
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
                note,
                deleted_at_utc
            FROM filtered_income
            ORDER BY deleted_at_utc DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        DynamicParameters parameters = new();
        parameters.Add("UserId", request.UserId);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        List<DeletedIncomeRow> rows = (await connection.QueryAsync<DeletedIncomeRow>(pageSql, parameters)).AsList();

        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        IReadOnlyCollection<DeletedIncomeListItemResponse> items = rows.Select(row =>
        {
            int daysUntilPermanentDeletion = Math.Max(0, softDeleteRetentionProvider.RetentionDays - (int)Math.Floor((dateTimeProvider.UtcNow - row.DeletedAtUtc).TotalDays));
            return new DeletedIncomeListItemResponse(
                row.Id,
                row.Amount,
                row.Currency,
                row.Date,
                row.Source,
                row.Type,
                row.Note,
                row.DeletedAtUtc,
                daysUntilPermanentDeletion);
        }).ToList();

        return new DeletedIncomePageResponse(page, pageSize, totalCount, page, totalPages, items);
    }

    private sealed class DeletedIncomeRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public decimal Amount { get; init; } = 0m;
        public string Currency { get; init; } = string.Empty;
        public DateOnly Date { get; init; } = DateOnly.MinValue;
        public string Source { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
        public DateTime DeletedAtUtc { get; init; } = DateTime.MinValue;
    }
}
