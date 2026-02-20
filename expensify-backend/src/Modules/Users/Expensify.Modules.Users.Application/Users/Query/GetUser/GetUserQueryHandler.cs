using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Application.Users.Query.GetUser;

internal sealed class GetUserQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUserQuery, GetUserResponse>
{
    public async Task<Result<GetUserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(GetUserResponse.Id)},
                 first_name AS {nameof(GetUserResponse.FirstName)},
                 last_name AS {nameof(GetUserResponse.LastName)}
             FROM users.users
             WHERE id = @Id
             """;

        GetUserResponse? user = await connection.QuerySingleOrDefaultAsync<GetUserResponse>(sql, request);

        if (user is null)
        {
            return Result.Failure<GetUserResponse>(UserErrors.NotFound(request.Id));
        }

        return user;
    }
}
