using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Application.Abstractions;

public sealed record RegisterUserRequest(string Email, string Password, string FirstName, string LastName, RoleType Role);

public sealed record RegisterUserResponse(Guid UserId);

public sealed record LoginUserResponse(string Token, string RefreshToken);

public sealed record RefreshTokenResponse(string Token, string RefreshToken);

public sealed record GetUserResponse(Guid Id, string FirstName, string LastName);
    
public sealed record GetAllUsersResponse(Guid Id,string Email, string FirstName, string LastName, string Role);
public sealed record GetUsersResponse(int Page,
    int PageSize,
    int TotalCount,
    int CurentPage,
    int TotalPages,
    IReadOnlyCollection<GetAllUsersResponse> Users);
