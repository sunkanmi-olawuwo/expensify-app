using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Domain.Incomes;

namespace Expensify.Modules.Income.Application.Incomes.Query.GetIncome;

internal sealed class GetIncomeQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetIncomeQuery, IncomeResponse>
{
    public async Task<Result<IncomeResponse>> Handle(GetIncomeQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT
                i.id,
                i.user_id,
                i.amount,
                i.currency,
                i.income_date AS date,
                i.source,
                i.type,
                i.note
            FROM income.incomes i
            WHERE i.id = @IncomeId AND i.user_id = @UserId
            """;

        IncomeRow? row = await connection.QuerySingleOrDefaultAsync<IncomeRow>(sql, request);
        if (row is null)
        {
            return Result.Failure<IncomeResponse>(IncomeErrors.NotFound(request.IncomeId));
        }

        return new IncomeResponse(
            row.Id,
            row.UserId,
            row.Amount,
            row.Currency,
            row.Date,
            row.Source,
            row.Type,
            row.Note);
    }

    private sealed class IncomeRow
    {
        public Guid Id { get; init; } = Guid.Empty;
        public Guid UserId { get; init; } = Guid.Empty;
        public decimal Amount { get; init; } = 0m;
        public string Currency { get; init; } = string.Empty;
        public DateOnly Date { get; init; } = DateOnly.MinValue;
        public string Source { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string Note { get; init; } = string.Empty;
    }
}
