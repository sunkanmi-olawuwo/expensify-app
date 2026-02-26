using Expensify.Common.Domain;

namespace Expensify.Common.Application.Authorization;

public interface IPermissionService
{
    Task<Result<PermissionsResponse>> GetUserPermissionsAsync(string identityId);
}
