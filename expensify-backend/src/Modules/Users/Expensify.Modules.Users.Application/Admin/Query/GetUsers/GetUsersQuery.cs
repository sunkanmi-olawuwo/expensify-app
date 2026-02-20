using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Admin.Query.GetUsers;

public sealed record GetUsersQuery(
    string FilterBy = "",
    string FilterQuery = "",
    string SortBy = "Email",
    string SearchQuery = "",
    int Page = 1,
    int PageSize = 10,
    string SortOrder = "asc"
) : IQuery<GetUsersResponse>;
