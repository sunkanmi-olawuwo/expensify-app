using Microsoft.AspNetCore.Authorization;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Modules.Income.Domain.Policies;

namespace Expensify.Modules.Income.Infrastructure.Policies;

internal sealed class IncomePolicyFactory : IPolicyFactory
{
    public string ModuleName => "Income";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [IncomePolicyConsts.ReadPolicy] = policy => policy.RequireClaim(IncomePolicyConsts.ReadPolicy),
            [IncomePolicyConsts.WritePolicy] = policy => policy.RequireClaim(IncomePolicyConsts.WritePolicy),
            [IncomePolicyConsts.DeletePolicy] = policy => policy.RequireClaim(IncomePolicyConsts.DeletePolicy),
            [IncomePolicyConsts.AdminReadPolicy] = policy => policy.RequireClaim(IncomePolicyConsts.AdminReadPolicy)
        };
    }
}
