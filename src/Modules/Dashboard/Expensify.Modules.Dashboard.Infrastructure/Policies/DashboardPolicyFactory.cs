using Microsoft.AspNetCore.Authorization;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Modules.Dashboard.Domain.Policies;

namespace Expensify.Modules.Dashboard.Infrastructure.Policies;

internal sealed class DashboardPolicyFactory : IPolicyFactory
{
    public string ModuleName => "Dashboard";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [DashboardPolicyConsts.ReadPolicy] = policy => policy.RequireClaim(DashboardPolicyConsts.ReadPolicy)
        };
    }
}
