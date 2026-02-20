namespace Expensify.Modules.Users.Domain.Policies;

public static class UserPolicyConsts
{
    public const string ReadPolicy =   "users:read";
    public const string UpdatePolicy = "users:update";
    public const string DeletePolicy = "admin:users:delete";
    public const string CreatePolicy = "admin:users:create";
    public const string ReadAllPolicy = "admin:users:read";
}
