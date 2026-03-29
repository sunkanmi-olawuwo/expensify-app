using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Domain.Accounts;

namespace Expensify.Modules.Investments.Application.Accounts.Query.GetInvestmentAccount;

internal sealed class GetInvestmentAccountQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetInvestmentAccountQuery, InvestmentAccountResponse>
{
    public async Task<Result<InvestmentAccountResponse>> Handle(GetInvestmentAccountQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT
                a.id AS Id,
                a.user_id AS UserId,
                a.name AS Name,
                a.provider AS Provider,
                a.category_id AS CategoryId,
                c.name AS CategoryName,
                c.slug AS CategorySlug,
                a.currency AS Currency,
                a.interest_rate AS InterestRate,
                a.maturity_date AS MaturityDate,
                a.current_balance AS CurrentBalance,
                a.notes AS Notes,
                a.created_at_utc AS CreatedAtUtc,
                a.updated_at_utc AS UpdatedAtUtc,
                COALESCE((
                    SELECT SUM(ic.amount)
                    FROM investments.investment_contributions ic
                    WHERE ic.investment_id = a.id
                      AND ic.deleted_at_utc IS NULL
                ), 0) AS TotalContributed
            FROM investments.investment_accounts a
            INNER JOIN investments.investment_categories c ON c.id = a.category_id
            WHERE a.id = @InvestmentId
              AND a.user_id = @UserId
              AND a.deleted_at_utc IS NULL
            """;

        InvestmentAccountRow? row = await connection.QuerySingleOrDefaultAsync<InvestmentAccountRow>(sql, request);
        if (row is null)
        {
            return Result.Failure<InvestmentAccountResponse>(InvestmentAccountErrors.NotFound(request.InvestmentId));
        }

        return new InvestmentAccountResponse(
            row.Id,
            row.UserId,
            row.Name,
            row.Provider,
            row.CategoryId,
            row.CategoryName,
            row.CategorySlug,
            row.Currency,
            row.InterestRate,
            row.MaturityDate,
            row.CurrentBalance,
            row.Notes,
            row.TotalContributed,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
    }

#pragma warning disable S3459, CA1805
    private sealed class InvestmentAccountRow
    {
        public Guid Id { get; init; } = Guid.Empty;

        public Guid UserId { get; init; } = Guid.Empty;

        public string Name { get; init; } = string.Empty;

        public string? Provider { get; init; } = null;

        public Guid CategoryId { get; init; } = Guid.Empty;

        public string CategoryName { get; init; } = string.Empty;

        public string CategorySlug { get; init; } = string.Empty;

        public string Currency { get; init; } = string.Empty;

        public decimal? InterestRate { get; init; } = null;

        public DateTimeOffset? MaturityDate { get; init; } = null;

        public decimal CurrentBalance { get; init; } = 0m;

        public string? Notes { get; init; } = null;

        public decimal TotalContributed { get; init; } = 0m;

        public DateTime CreatedAtUtc { get; init; } = default;

        public DateTime? UpdatedAtUtc { get; init; } = null;
    }
#pragma warning restore S3459, CA1805
}
