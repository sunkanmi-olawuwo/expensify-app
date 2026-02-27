using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;
using Expensify.Modules.Expenses.Domain.Expenses;

namespace Expensify.Modules.Expenses.Application.Expenses.Query.GetExpense;

internal sealed class GetExpenseQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetExpenseQuery, ExpenseResponse>
{
    public async Task<Result<ExpenseResponse>> Handle(GetExpenseQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT
                e.id,
                e.user_id,
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
            WHERE e.id = @ExpenseId AND e.user_id = @UserId AND e.deleted_at_utc IS NULL
            """;

        ExpenseRow? row = await connection.QuerySingleOrDefaultAsync<ExpenseRow>(sql, request);
        if (row is null)
        {
            return Result.Failure<ExpenseResponse>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        const string tagsSql =
            """
            SELECT t.id, t.name
            FROM expenses.expense_expense_tags et
            INNER JOIN expenses.expense_tags t ON t.id = et.tags_id
            WHERE et.expense_id = @ExpenseId
            ORDER BY t.name
            """;

        List<TagRow> tags = (await connection.QueryAsync<TagRow>(tagsSql, request)).AsList();

        return new ExpenseResponse(
            row.Id,
            row.UserId,
            row.Amount,
            row.Currency,
            row.Date,
            row.CategoryId,
            row.CategoryName,
            row.Merchant,
            row.Note,
            row.PaymentMethod,
            tags.Select(t => t.Id).ToList(),
            tags.Select(t => t.Name).ToList());
    }

    private sealed class ExpenseRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public Guid UserId { get; init; } = Guid.Empty;
        public decimal Amount { get; init; } = 0m;
        public string Currency { get; init; } = string.Empty;
        public DateOnly Date { get; init; } = DateOnly.MinValue;
        public Guid CategoryId { get; init; } = Guid.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public string Merchant { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
        public string PaymentMethod { get; init; } = string.Empty;
    }

    private sealed class TagRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public string Name { get; init; } = string.Empty;
    }
}
