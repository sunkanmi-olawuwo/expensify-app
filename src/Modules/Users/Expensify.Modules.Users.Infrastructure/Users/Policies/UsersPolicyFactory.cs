using Microsoft.AspNetCore.Authorization;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Modules.Users.Domain.Policies;

namespace Expensify.Modules.Users.Infrastructure.Users.Policies;

internal sealed class UsersPolicyFactory : IPolicyFactory
{
    public string ModuleName => "Users";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [UserPolicyConsts.ReadPolicy] = policy => policy.RequireClaim(UserPolicyConsts.ReadPolicy),
            [UserPolicyConsts.ReadAllPolicy] = policy => policy.RequireClaim(UserPolicyConsts.ReadAllPolicy),
            [UserPolicyConsts.CreatePolicy] = policy => policy.RequireClaim(UserPolicyConsts.CreatePolicy),
            [UserPolicyConsts.UpdatePolicy] = policy => policy.RequireClaim(UserPolicyConsts.UpdatePolicy),
            [UserPolicyConsts.DeletePolicy] = policy => policy.RequireClaim(UserPolicyConsts.DeletePolicy)
        };
    }
}
