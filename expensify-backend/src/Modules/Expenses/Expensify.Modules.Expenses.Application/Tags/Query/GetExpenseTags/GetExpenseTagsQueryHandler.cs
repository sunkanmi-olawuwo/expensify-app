using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Expenses.Application.Abstractions;

namespace Expensify.Modules.Expenses.Application.Tags.Query.GetExpenseTags;

internal sealed class GetExpenseTagsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetExpenseTagsQuery, IReadOnlyCollection<ExpenseTagResponse>>
{
    public async Task<Result<IReadOnlyCollection<ExpenseTagResponse>>> Handle(GetExpenseTagsQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT id, user_id, name
            FROM expenses.expense_tags
            WHERE user_id = @UserId
            ORDER BY name
            """;

        List<ExpenseTagRow> rows = (await connection.QueryAsync<ExpenseTagRow>(sql, request)).AsList();

        IReadOnlyCollection<ExpenseTagResponse> response = rows
            .Select(r => new ExpenseTagResponse(r.Id, r.UserId, r.Name))
            .ToList();

        return Result.Success(response);
    }

    #pragma warning disable S3459
    private sealed class ExpenseTagRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string Name { get; init; } = string.Empty;
    }
    #pragma warning restore S3459
}
