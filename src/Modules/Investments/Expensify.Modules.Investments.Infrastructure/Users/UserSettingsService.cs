using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions.Users;

namespace Expensify.Modules.Investments.Infrastructure.Users;

internal sealed class UserSettingsService(IDbConnectionFactory dbConnectionFactory) : IUserSettingsService
{
    public async Task<Result<UserSettingsResponse>> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT
                currency AS Currency,
                timezone AS Timezone,
                month_start_day AS MonthStartDay
            FROM users.users
            WHERE id = @UserId
            """;

        UserSettingsRow? row = await connection.QuerySingleOrDefaultAsync<UserSettingsRow>(sql, new { UserId = userId });
        if (row is null)
        {
            return Result.Failure<UserSettingsResponse>(
                Error.NotFound("Investments.UserSettingsNotFound", $"User settings for user '{userId}' were not found"));
        }

        return new UserSettingsResponse(row.Currency, row.Timezone, row.MonthStartDay);
    }

    private sealed record UserSettingsRow(string Currency, string Timezone, int MonthStartDay);
}
