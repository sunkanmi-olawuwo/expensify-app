using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Application.Abstractions;

namespace Expensify.Modules.Investments.Application.Categories.Query.GetInvestmentCategories;

internal sealed class GetInvestmentCategoriesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetInvestmentCategoriesQuery, IReadOnlyCollection<InvestmentCategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<InvestmentCategoryResponse>>> Handle(GetInvestmentCategoriesQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            """
            SELECT
                id AS Id,
                name AS Name,
                slug AS Slug,
                is_active AS IsActive
            FROM investments.investment_categories
            WHERE (@IncludeInactive::boolean = TRUE OR is_active = TRUE)
            ORDER BY name
            """;

        IReadOnlyCollection<InvestmentCategoryResponse> categories =
            (await connection.QueryAsync<InvestmentCategoryResponse>(sql, new { request.IncludeInactive })).ToList();

        return Result.Success(categories);
    }
}
