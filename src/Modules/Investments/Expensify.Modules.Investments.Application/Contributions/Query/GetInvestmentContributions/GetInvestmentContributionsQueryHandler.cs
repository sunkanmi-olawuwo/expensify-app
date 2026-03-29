using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Domain.Accounts;

namespace Expensify.Modules.Investments.Application.Contributions.Query.GetInvestmentContributions;

internal sealed class GetInvestmentContributionsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetInvestmentContributionsQuery, InvestmentContributionsPageResponse>
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<InvestmentContributionsPageResponse>> Handle(GetInvestmentContributionsQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string ownershipSql =
            """
            SELECT EXISTS(
                SELECT 1
                FROM investments.investment_accounts
                WHERE id = @InvestmentId
                  AND user_id = @UserId
                  AND deleted_at_utc IS NULL
            )
            """;

        bool exists = await connection.ExecuteScalarAsync<bool>(ownershipSql, request);
        if (!exists)
        {
            return Result.Failure<InvestmentContributionsPageResponse>(InvestmentAccountErrors.NotFound(request.InvestmentId));
        }

        int page = request.Page > 0 ? request.Page : DefaultPage;
        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, MaxPageSize) : DefaultPageSize;
        int offset = (page - 1) * pageSize;

        const string countSql =
            """
            SELECT COUNT(*)
            FROM investments.investment_contributions
            WHERE investment_id = @InvestmentId
              AND deleted_at_utc IS NULL
            """;

        const string pageSql =
            """
            SELECT
                id AS Id,
                investment_id AS InvestmentId,
                amount AS Amount,
                date AS Date,
                notes AS Notes,
                created_at_utc AS CreatedAtUtc
            FROM investments.investment_contributions
            WHERE investment_id = @InvestmentId
              AND deleted_at_utc IS NULL
            ORDER BY date DESC, created_at_utc DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, request);
        List<InvestmentContributionRow> rows = (await connection.QueryAsync<InvestmentContributionRow>(
            pageSql,
            new
            {
                request.InvestmentId,
                PageSize = pageSize,
                Offset = offset
            })).AsList();

        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        IReadOnlyCollection<InvestmentContributionResponse> items = rows.Select(row =>
            new InvestmentContributionResponse(
                row.Id,
                row.InvestmentId,
                row.Amount,
                row.Date,
                row.Notes,
                row.CreatedAtUtc))
            .ToList();

        return new InvestmentContributionsPageResponse(page, pageSize, totalCount, totalPages, items);
    }

#pragma warning disable S3459, CA1805
    private sealed class InvestmentContributionRow
    {
        public Guid Id { get; init; } = Guid.Empty;

        public Guid InvestmentId { get; init; } = Guid.Empty;

        public decimal Amount { get; init; } = 0m;

        public DateTimeOffset Date { get; init; } = default;

        public string? Notes { get; init; } = null;

        public DateTime CreatedAtUtc { get; init; } = default;
    }
#pragma warning restore S3459, CA1805
}
