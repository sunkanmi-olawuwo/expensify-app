using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Users.Query.GetTimezones;

internal sealed class GetTimezonesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetTimezonesQuery, IReadOnlyCollection<TimezoneResponse>>
{
    public async Task<Result<IReadOnlyCollection<TimezoneResponse>>> Handle(GetTimezonesQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        string sql =
            $"""
             SELECT
                 iana_id AS {nameof(TimezoneResponse.IanaId)},
                 display_name AS {nameof(TimezoneResponse.DisplayName)},
                 is_active AS {nameof(TimezoneResponse.IsActive)},
                 is_default AS {nameof(TimezoneResponse.IsDefault)},
                 sort_order AS {nameof(TimezoneResponse.SortOrder)}
             FROM users.timezones
             WHERE @IncludeInactive OR is_active
             ORDER BY sort_order, display_name, iana_id
             """;

        IReadOnlyCollection<TimezoneResponse> timezones =
            (await connection.QueryAsync<TimezoneResponse>(sql, new { request.IncludeInactive }))
            .AsList();

        return Result.Success(timezones);
    }
}
