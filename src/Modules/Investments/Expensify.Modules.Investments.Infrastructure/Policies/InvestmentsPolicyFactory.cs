using Microsoft.AspNetCore.Authorization;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Modules.Investments.Domain.Policies;

namespace Expensify.Modules.Investments.Infrastructure.Policies;

internal sealed class InvestmentsPolicyFactory : IPolicyFactory
{
    public string ModuleName => "Investments";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [InvestmentPolicyConsts.ReadPolicy] = policy => policy.RequireClaim(InvestmentPolicyConsts.ReadPolicy),
            [InvestmentPolicyConsts.WritePolicy] = policy => policy.RequireClaim(InvestmentPolicyConsts.WritePolicy),
            [InvestmentPolicyConsts.DeletePolicy] = policy => policy.RequireClaim(InvestmentPolicyConsts.DeletePolicy),
            [InvestmentPolicyConsts.AdminReadPolicy] = policy => policy.RequireClaim(InvestmentPolicyConsts.AdminReadPolicy),
            [InvestmentPolicyConsts.AdminManageCategoriesPolicy] = policy => policy.RequireClaim(InvestmentPolicyConsts.AdminManageCategoriesPolicy)
        };
    }
}
