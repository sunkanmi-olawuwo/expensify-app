using Microsoft.AspNetCore.Identity;
using Expensify.Common.Domain;

namespace Expensify.Modules.Users.Domain.Users;

public static class UserErrors
{
    private const string ErrorPrefix = "Users";

    public static Error NotFound(Guid userId) =>
        Error.NotFound("Users.NotFound", $"The user with the identifier {userId} not found");

    public static Error NotFound(string identityId) =>
        Error.NotFound("Users.NotFound", $"The user with the IDP identifier {identityId} not found");

    public static Error NotFoundByEmail(string email) =>
       Error.NotFound($"{ErrorPrefix}.{nameof(NotFound)}", $"User with email {email} not found");

    public static Error RegistrationFailed(IEnumerable<IdentityError> identityErrors) =>
       Error.Validation($"{ErrorPrefix}.{nameof(RegistrationFailed)}", string.Join(", ", identityErrors.Select(e => e.Description)));

    public static Error InvalidCredentials() =>
       Error.Validation($"{ErrorPrefix}.{nameof(InvalidCredentials)}", "Invalid email or password");

    public static Error InvalidToken() =>
      Error.Validation($"{ErrorPrefix}.{nameof(InvalidToken)}", "Invalid token");

    public static Error RoleNotFound(string roleName) =>
      Error.NotFound($"{ErrorPrefix}.{nameof(RoleNotFound)}", $"Role '{roleName}' not found");

    public static Error UpdateRoleFailed(IEnumerable<IdentityError> identityErrors) =>
        Error.NotFound($"{ErrorPrefix}.{nameof(UpdateRoleFailed)}", string.Join(", ", identityErrors.Select(e => e.Description)));

    public static Error DeleteUserFailed(IEnumerable<IdentityError> identityErrors) =>  
        Error.NotFound($"{ErrorPrefix}.{nameof(DeleteUserFailed)}", string.Join(", ", identityErrors.Select(e => e.Description)));
}
