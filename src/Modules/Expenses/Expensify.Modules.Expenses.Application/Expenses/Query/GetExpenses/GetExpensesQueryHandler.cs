using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.Users;
using Expensify.Modules.Expenses.Application.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetExpenses;

internal sealed class GetExpensesQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserSettingsService userSettingsService)
    : IQueryHandler<GetExpensesQuery, ExpensesPageResponse>
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<ExpensesPageResponse>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<ExpensesPageResponse>(userSettingsResult.Error);
        }

        Result<MonthPeriod> periodResult = MonthPeriod.Create(request.Period, userSettingsResult.Value.MonthStartDay);
        if (periodResult.IsFailure)
        {
            return Result.Failure<ExpensesPageResponse>(periodResult.Error);
        }

        int page = request.Page > 0 ? request.Page : DefaultPage;
        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, MaxPageSize) : DefaultPageSize;
        int offset = (page - 1) * pageSize;
        var periodStart = periodResult.Value.StartDate.ToDateTime(TimeOnly.MinValue);
        var periodEndExclusive = periodResult.Value.EndDateExclusive.ToDateTime(TimeOnly.MinValue);

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        string orderByColumn = ResolveSortColumn(request.SortBy);
        string orderDirection = ResolveSortOrder(request.SortOrder);
        Guid[]? filterTagIds = request.TagIds?.Distinct().ToArray();
        bool hasTagFilter = filterTagIds is { Length: > 0 };
        string merchantSearch = string.IsNullOrWhiteSpace(request.Merchant)
            ? string.Empty
            : $"%{request.Merchant.Trim()}%";
        string paymentMethod = request.PaymentMethod?.Trim() ?? string.Empty;

        string filteredExpensesCte =
            $"""
            WITH filtered_expenses AS (
                SELECT
                    e.id,
                    e.amount,
                    e.currency,
                    e.expense_date AS date,
                    e.category_id,
                    c.name AS category_name,
                    e.merchant,
                    e.note,
                    e.payment_method
                FROM expenses.expenses e
                INNER JOIN expenses.expense_categories c ON c.id = e.category_id
                WHERE e.user_id = @UserId
                  AND e.deleted_at_utc IS NULL
                  AND e.expense_date >= @StartDate
                  AND e.expense_date < @EndDateExclusive
                  AND (@CategoryId::uuid IS NULL OR e.category_id = @CategoryId::uuid)
                  AND (@Merchant = '' OR e.merchant ILIKE @Merchant)
                  AND (@MinAmount::numeric IS NULL OR e.amount >= @MinAmount::numeric)
                  AND (@MaxAmount::numeric IS NULL OR e.amount <= @MaxAmount::numeric)
                  AND (@PaymentMethod = '' OR e.payment_method = @PaymentMethod)
                  AND (
                    @HasTagFilter::boolean = FALSE OR EXISTS (
                        SELECT 1
                        FROM expenses.expense_expense_tags et
                        WHERE et.expense_id = e.id
                          AND et.tags_id = ANY(@TagIds::uuid[])
                    )
                  )
            )
            """;

        string countSql =
            $"""
            {filteredExpensesCte}
            SELECT COUNT(*)
            FROM filtered_expenses
            """;

        string pageSql =
            $"""
            {filteredExpensesCte}
            SELECT
                id,
                amount,
                currency,
                date,
                category_id,
                category_name,
                merchant,
                note,
                payment_method
            FROM filtered_expenses
            ORDER BY {orderByColumn} {orderDirection}
            LIMIT @PageSize OFFSET @Offset
            """;

        DynamicParameters parameters = new();
        parameters.Add("UserId", request.UserId);
        parameters.Add("StartDate", periodStart);
        parameters.Add("EndDateExclusive", periodEndExclusive);
        parameters.Add("CategoryId", request.CategoryId);
        parameters.Add("Merchant", merchantSearch);
        parameters.Add("MinAmount", request.MinAmount);
        parameters.Add("MaxAmount", request.MaxAmount);
        parameters.Add("PaymentMethod", paymentMethod);
        parameters.Add("HasTagFilter", hasTagFilter);
        parameters.Add("TagIds", filterTagIds);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        List<ExpenseRow> rows = (await connection.QueryAsync<ExpenseRow>(pageSql, parameters)).AsList();

        Guid[] expenseIds = rows.Select(r => r.Id).ToArray();
        Dictionary<Guid, List<TagRow>> tagsByExpense = await LoadTagsByExpenseAsync(connection, expenseIds);

        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        IReadOnlyCollection<ExpenseListItemResponse> items = rows.Select(row =>
        {
            List<TagRow> tagRows = tagsByExpense.GetValueOrDefault(row.Id, []);
            return new ExpenseListItemResponse(
                row.Id,
                row.Amount,
                row.Currency,
                row.Date,
                row.CategoryId,
                row.CategoryName,
                row.Merchant,
                row.Note,
                row.PaymentMethod,
                tagRows.Select(t => t.Id).ToList(),
                tagRows.Select(t => t.Name).ToList());
        }).ToList();

        return new ExpensesPageResponse(page, pageSize, totalCount, page, totalPages, items);
    }

    private static async Task<Dictionary<Guid, List<TagRow>>> LoadTagsByExpenseAsync(DbConnection connection, Guid[] expenseIds)
    {
        if (expenseIds.Length == 0)
        {
            return [];
        }

        const string tagsSql =
            """
            SELECT et.expense_id, t.id, t.name
            FROM expenses.expense_expense_tags et
            INNER JOIN expenses.expense_tags t ON t.id = et.tags_id
            WHERE et.expense_id = ANY(@ExpenseIds)
            ORDER BY t.name
            """;

        List<ExpenseTagJoinRow> rows = (await connection.QueryAsync<ExpenseTagJoinRow>(tagsSql, new { ExpenseIds = expenseIds })).AsList();

        return rows
            .GroupBy(r => r.ExpenseId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new TagRow { Id = t.Id, Name = t.Name }).ToList());
    }

    private static string ResolveSortColumn(string sortBy) =>
        sortBy.Trim().ToLowerInvariant() switch
        {
            "amount" => "amount",
            "merchant" => "merchant",
            _ => "date"
        };

    private static string ResolveSortOrder(string sortOrder) =>
        sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

    private sealed class ExpenseRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public decimal Amount { get; init; } = 0m;
        public string Currency { get; init; } = string.Empty;
        public DateOnly Date { get; init; } = DateOnly.MinValue;
        public Guid CategoryId { get; init; } = Guid.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public string Merchant { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
        public string PaymentMethod { get; init; } = string.Empty;
    }

    private sealed class ExpenseTagJoinRow
    {
        public Guid ExpenseId { get; init; } = Guid.Empty;
        public Guid Id { get; init; } = Guid.Empty;
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TagRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public string Name { get; init; } = string.Empty;
    }
}
