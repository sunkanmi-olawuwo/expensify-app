using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Domain;
using Expensify.Modules.Income.Application.Abstractions.Users;

namespace Expensify.Modules.Income.Infrastructure.Users;

internal sealed class UserSettingsService(IDbConnectionFactory dbConnectionFactory) : IUserSettingsService
{
    public async Task<Result<UserSettingsResponse>> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT currency, timezone, month_start_day
            FROM users.users
            WHERE id = @UserId
            """;

        UserSettingsRow? row = await connection.QuerySingleOrDefaultAsync<UserSettingsRow>(sql, new { UserId = userId });

        if (row is null)
        {
            return Result.Failure<UserSettingsResponse>(
                Error.NotFound("Income.UserSettingsNotFound", $"User settings for user '{userId}' were not found"));
        }

        return new UserSettingsResponse(row.Currency, row.Timezone, row.MonthStartDay);
    }

    private sealed class UserSettingsRow
    {
        public string Currency { get; init; } = string.Empty;
        public string Timezone { get; init; } = string.Empty;
        public int MonthStartDay { get; init; } = default!;
    }
}
