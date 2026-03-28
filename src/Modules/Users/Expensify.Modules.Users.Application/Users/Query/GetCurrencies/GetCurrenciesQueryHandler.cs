using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Users.Query.GetCurrencies;

internal sealed class GetCurrenciesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetCurrenciesQuery, IReadOnlyCollection<CurrencyResponse>>
{
    public async Task<Result<IReadOnlyCollection<CurrencyResponse>>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        string sql =
            $"""
             SELECT
                 code AS {nameof(CurrencyResponse.Code)},
                 name AS {nameof(CurrencyResponse.Name)},
                 symbol AS {nameof(CurrencyResponse.Symbol)},
                 minor_unit AS {nameof(CurrencyResponse.MinorUnit)},
                 is_active AS {nameof(CurrencyResponse.IsActive)},
                 is_default AS {nameof(CurrencyResponse.IsDefault)},
                 sort_order AS {nameof(CurrencyResponse.SortOrder)}
             FROM users.currencies
             WHERE @IncludeInactive OR is_active
             ORDER BY sort_order, name, code
             """;

        IReadOnlyCollection<CurrencyResponse> currencies =
            (await connection.QueryAsync<CurrencyResponse>(sql, new { request.IncludeInactive }))
            .AsList();

        return Result.Success(currencies);
    }
}
