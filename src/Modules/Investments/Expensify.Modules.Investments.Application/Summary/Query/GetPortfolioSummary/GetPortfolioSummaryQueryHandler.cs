using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;
using Expensify.Modules.Investments.Application.Abstractions.Users;

namespace Expensify.Modules.Investments.Application.Summary.Query.GetPortfolioSummary;

internal sealed class GetPortfolioSummaryQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserSettingsService userSettingsService)
    : IQueryHandler<GetPortfolioSummaryQuery, PortfolioSummaryResponse>
{
    public async Task<Result<PortfolioSummaryResponse>> Handle(GetPortfolioSummaryQuery request, CancellationToken cancellationToken)
    {
        Result<UserSettingsResponse> userSettingsResult = await userSettingsService.GetSettingsAsync(request.UserId, cancellationToken);
        if (userSettingsResult.IsFailure)
        {
            return Result.Failure<PortfolioSummaryResponse>(userSettingsResult.Error);
        }

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT
                COALESCE((
                    SELECT SUM(ic.amount)
                    FROM investments.investment_contributions ic
                    INNER JOIN investments.investment_accounts ia ON ia.id = ic.investment_id
                    WHERE ia.user_id = @UserId
                      AND ia.deleted_at_utc IS NULL
                      AND ic.deleted_at_utc IS NULL
                ), 0) AS TotalContributed,
                COALESCE((
                    SELECT SUM(ia.current_balance)
                    FROM investments.investment_accounts ia
                    WHERE ia.user_id = @UserId
                      AND ia.deleted_at_utc IS NULL
                ), 0) AS CurrentValue,
                (
                    SELECT COUNT(*)
                    FROM investments.investment_accounts ia
                    WHERE ia.user_id = @UserId
                      AND ia.deleted_at_utc IS NULL
                ) AS AccountCount
            """;

        SummaryRow row = await connection.QuerySingleAsync<SummaryRow>(sql, request);

        decimal totalContributed = row.TotalContributed;
        decimal currentValue = row.CurrentValue;
        int accountCount = row.AccountCount;

        decimal totalGainLoss = currentValue - totalContributed;
        decimal gainLossPercentage = totalContributed == 0
            ? 0
            : totalGainLoss / Math.Abs(totalContributed) * 100;

        return new PortfolioSummaryResponse(
            totalContributed,
            currentValue,
            totalGainLoss,
            gainLossPercentage,
            accountCount,
            userSettingsResult.Value.Currency);
    }

#pragma warning disable S3459, CA1805
    private sealed class SummaryRow
    {
        public decimal TotalContributed { get; init; } = 0m;

        public decimal CurrentValue { get; init; } = 0m;

        public int AccountCount { get; init; } = 0;
    }
#pragma warning restore S3459, CA1805
}
