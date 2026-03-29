using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Accounts.Query.GetInvestmentAccounts;

internal sealed class GetInvestmentAccountsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetInvestmentAccountsQuery, InvestmentAccountsPageResponse>
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<Result<InvestmentAccountsPageResponse>> Handle(GetInvestmentAccountsQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page > 0 ? request.Page : DefaultPage;
        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, MaxPageSize) : DefaultPageSize;
        int offset = (page - 1) * pageSize;

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string baseSql =
            """
            FROM investments.investment_accounts a
            INNER JOIN investments.investment_categories c ON c.id = a.category_id
            WHERE a.user_id = @UserId
              AND a.deleted_at_utc IS NULL
              AND (@CategoryId::uuid IS NULL OR a.category_id = @CategoryId::uuid)
            """;

        string countSql =
            $"""
            SELECT COUNT(*)
            {baseSql}
            """;

        string pageSql =
            $"""
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
                a.updated_at_utc AS UpdatedAtUtc
            {baseSql}
            ORDER BY COALESCE(a.updated_at_utc, a.created_at_utc) DESC, a.created_at_utc DESC
            LIMIT @PageSize OFFSET @Offset
            """;

        DynamicParameters parameters = new();
        parameters.Add("UserId", request.UserId);
        parameters.Add("CategoryId", request.CategoryId);
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);

        int totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        List<InvestmentAccountRow> rows = (await connection.QueryAsync<InvestmentAccountRow>(pageSql, parameters)).AsList();
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        IReadOnlyCollection<InvestmentAccountListItemResponse> items = rows.Select(row =>
            new InvestmentAccountListItemResponse(
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
                row.CreatedAtUtc,
                row.UpdatedAtUtc))
            .ToList();

        return new InvestmentAccountsPageResponse(page, pageSize, totalCount, totalPages, items);
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

        public DateTime CreatedAtUtc { get; init; } = default;

        public DateTime? UpdatedAtUtc { get; init; } = null;
    }
#pragma warning restore S3459, CA1805
}
