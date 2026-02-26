using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Categories.Query.GetExpenseCategories;

internal sealed class GetExpenseCategoriesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetExpenseCategoriesQuery, IReadOnlyCollection<ExpenseCategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ExpenseCategoryResponse>>> Handle(GetExpenseCategoriesQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT id, user_id, name
            FROM expenses.expense_categories
            WHERE user_id = @UserId
            ORDER BY name
            """;

        List<ExpenseCategoryRow> rows = (await connection.QueryAsync<ExpenseCategoryRow>(sql, request)).AsList();

        IReadOnlyCollection<ExpenseCategoryResponse> response = rows
            .Select(r => new ExpenseCategoryResponse(r.Id, r.UserId, r.Name))
            .ToList();

        return Result.Success(response);
    }

    private sealed class ExpenseCategoryRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public Guid UserId { get; init; } = Guid.Empty;
        public string Name { get; init; } = string.Empty;
    }
}
