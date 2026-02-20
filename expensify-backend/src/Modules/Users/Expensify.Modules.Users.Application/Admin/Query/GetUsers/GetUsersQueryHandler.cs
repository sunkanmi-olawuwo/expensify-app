using System.Data.Common;
using Dapper;
using Expensify.Common.Application.Data;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Admin.Query.GetUsers;

internal sealed class GetUsersQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetUsersQuery, GetUsersResponse>
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    public async Task<Result<GetUsersResponse>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        int page = request.Page > 0 ? request.Page : DefaultPage;
        int pageSize = request.PageSize > 0 ? Math.Min(request.PageSize, MaxPageSize) : DefaultPageSize;

        string? roleFilter = ResolveRoleFilter(request.FilterBy, request.FilterQuery);
        string? searchQuery = string.IsNullOrWhiteSpace(request.SearchQuery)
            ? null
            : $"%{request.SearchQuery.Trim()}%";
        string? normalizedSearchQuery = string.IsNullOrWhiteSpace(request.SearchQuery)
            ? null
            : $"%{request.SearchQuery.Trim().ToLowerInvariant()}%";

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        bool isPostgres = connection.GetType().Name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        string searchPredicate = isPostgres
            ? """
              @SearchQuery IS NULL
              OR iu.email ILIKE @SearchQuery
              OR u.last_name ILIKE @SearchQuery
              OR u.first_name ILIKE @SearchQuery
              """
            : """
              @NormalizedSearchQuery IS NULL
              OR LOWER(iu.email) LIKE @NormalizedSearchQuery
              OR LOWER(u.last_name) LIKE @NormalizedSearchQuery
              OR LOWER(u.first_name) LIKE @NormalizedSearchQuery
              """;
        string sortColumn = ResolveSortColumn(request.SortBy);
        string sortOrder = ResolveSortOrder(request.SortOrder);
        int offset = (page - 1) * pageSize;

        string usersSql =
            $"""
             WITH filtered_users AS (
                 SELECT
                     u.id AS id,
                     iu.email AS email,
                     u.first_name AS first_name,
                     u.last_name AS last_name,
                     COALESCE(MIN(r.name), '') AS role
                 FROM users.users u
                 INNER JOIN users.identity_users iu ON iu.id = u.identity_id
                 LEFT JOIN users.user_roles ur ON ur.user_id = iu.id
                 LEFT JOIN users.roles r ON r.id = ur.role_id
                 WHERE (@RoleFilter IS NULL OR r.name = @RoleFilter)
                   AND ({searchPredicate})
                 GROUP BY u.id, iu.email, u.first_name, u.last_name
             )
             SELECT
                 CAST(id AS TEXT) AS {nameof(GetUsersRow.Id)},
                 email AS {nameof(GetUsersRow.Email)},
                 first_name AS {nameof(GetUsersRow.FirstName)},
                 last_name AS {nameof(GetUsersRow.LastName)},
                 role AS {nameof(GetUsersRow.Role)},
                 COUNT(*) OVER() AS {nameof(GetUsersRow.TotalCount)}
             FROM filtered_users
             ORDER BY {sortColumn} {sortOrder}
             LIMIT @PageSize OFFSET @Offset
             """;

        List<GetUsersRow> rows = (await connection.QueryAsync<GetUsersRow>(usersSql, new
        {
            RoleFilter = roleFilter,
            SearchQuery = searchQuery,
            NormalizedSearchQuery = normalizedSearchQuery,
            PageSize = pageSize,
            Offset = offset
        })).AsList();

        int totalCount = rows.Count > 0
            ? checked((int)rows[0].TotalCount)
            : await GetTotalCountAsync(connection, roleFilter, searchQuery, normalizedSearchQuery, searchPredicate);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        IReadOnlyCollection<GetAllUsersResponse> users = rows
            .Select(row => new GetAllUsersResponse(Guid.Parse(row.Id), row.Email, row.FirstName, row.LastName, row.Role))
            .ToList();

        return new GetUsersResponse(
            page,
            pageSize,
            totalCount,
            page,
            totalPages,
            users);
    }

    private static async Task<int> GetTotalCountAsync(
        DbConnection connection,
        string? roleFilter,
        string? searchQuery,
        string? normalizedSearchQuery,
        string searchPredicate)
    {
        string countSql =
            $"""
             SELECT COUNT(*)
             FROM (
                 SELECT u.id
                 FROM users.users u
                 INNER JOIN users.identity_users iu ON iu.id = u.identity_id
                 LEFT JOIN users.user_roles ur ON ur.user_id = iu.id
                 LEFT JOIN users.roles r ON r.id = ur.role_id
                 WHERE (@RoleFilter IS NULL OR r.name = @RoleFilter)
                   AND ({searchPredicate})
                 GROUP BY u.id, iu.email, u.first_name, u.last_name
             ) filtered_users
             """;

        return await connection.ExecuteScalarAsync<int>(countSql, new
        {
            RoleFilter = roleFilter,
            SearchQuery = searchQuery,
            NormalizedSearchQuery = normalizedSearchQuery
        });
    }

    private static string? ResolveRoleFilter(string filterBy, string filterQuery)
    {
        if (!filterBy.Equals("role", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(filterQuery))
        {
            return null;
        }

        return filterQuery.Trim();
    }

    private static string ResolveSortColumn(string sortBy) =>
        sortBy.Trim().ToLowerInvariant() switch
        {
            "email" => "email",
            "lastname" => "last_name",
            "firstname" => "first_name",
            "role" => "role",
            _ => "email"
        };

    private static string ResolveSortOrder(string sortOrder) =>
        sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

#pragma warning disable S3459
    private sealed class GetUsersRow
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public long TotalCount { get; set; }
    }
#pragma warning restore S3459
}
