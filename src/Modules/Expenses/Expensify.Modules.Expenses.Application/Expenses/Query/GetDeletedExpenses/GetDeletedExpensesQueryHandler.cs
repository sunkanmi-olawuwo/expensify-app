using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Clock;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Application.Abstractions.SoftDelete;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetDeletedExpenses;

internal sealed class GetDeletedExpensesQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IDateTimeProvider dateTimeProvider,
    ISoftDeleteRetentionProvider softDeleteRetentionProvider) : IQueryHandler<GetDeletedExpensesQuery, DeletedExpensesPageResponse>
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<DeletedExpensesPageResponse>> Handle(GetDeletedExpensesQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page > 0 ? request.Page : DefaultPage;
        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, MaxPageSize) : DefaultPageSize;
        int offset = (page - 1) * pageSize;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string filteredExpensesCte =
            """
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
                    e.payment_method,
                    e.deleted_at_utc
                FROM expenses.expenses e
                INNER JOIN expenses.expense_categories c ON c.id = e.category_id
                WHERE e.user_id = @UserId
                  AND e.deleted_at_utc IS NOT NULL
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
                payment_method,
                deleted_at_utc
            FROM filtered_expenses
            ORDER BY deleted_at_utc DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        DynamicParameters parameters = new();
        parameters.Add("UserId", request.UserId);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        List<DeletedExpenseRow> rows = (await connection.QueryAsync<DeletedExpenseRow>(pageSql, parameters)).AsList();

        Guid[] expenseIds = rows.Select(r => r.Id).ToArray();
        Dictionary<Guid, List<TagRow>> tagsByExpense = await LoadTagsByExpenseAsync(connection, expenseIds);

        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        IReadOnlyCollection<DeletedExpenseListItemResponse> items = rows.Select(row =>
        {
            List<TagRow> tagRows = tagsByExpense.GetValueOrDefault(row.Id, []);
            int daysUntilPermanentDeletion = Math.Max(0, softDeleteRetentionProvider.RetentionDays - (int)Math.Floor((dateTimeProvider.UtcNow - row.DeletedAtUtc).TotalDays));

            return new DeletedExpenseListItemResponse(
                row.Id,
                row.Amount,
                row.Currency,
                row.Date,
                row.CategoryId,
                row.CategoryName,
                row.Merchant,
                row.Note,
                row.PaymentMethod,
                row.DeletedAtUtc,
                daysUntilPermanentDeletion,
                tagRows.Select(t => t.Id).ToList(),
                tagRows.Select(t => t.Name).ToList());
        }).ToList();

        return new DeletedExpensesPageResponse(page, pageSize, totalCount, page, totalPages, items);
    }

    private static async Task<Dictionary<Guid, List<TagRow>>> LoadTagsByExpenseAsync(DbConnection connection, Guid[] expenseIds)
    {
        if (expenseIds.Length == 0)
        {
            return [];
        }

        string tagsSql = connection.GetType().Name switch
        {
            "NpgsqlConnection" =>
            """
            SELECT et.expense_id, t.id, t.name
            FROM expenses.expense_expense_tags et
            INNER JOIN expenses.expense_tags t ON t.id = et.tags_id
            WHERE et.expense_id = ANY(@ExpenseIds)
            ORDER BY t.name
            """,
            _ =>
            """
            SELECT et.expense_id, t.id, t.name
            FROM expenses.expense_expense_tags et
            INNER JOIN expenses.expense_tags t ON t.id = et.tags_id
            WHERE et.expense_id IN @ExpenseIds
            ORDER BY t.name
            """
        };

        List<ExpenseTagJoinRow> rows = (await connection.QueryAsync<ExpenseTagJoinRow>(tagsSql, new { ExpenseIds = expenseIds })).AsList();

        return rows
            .GroupBy(r => r.ExpenseId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => new TagRow { Id = t.Id, Name = t.Name }).ToList());
    }

    private sealed class DeletedExpenseRow
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
        public DateTime DeletedAtUtc { get; init; } = DateTime.MinValue;
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
