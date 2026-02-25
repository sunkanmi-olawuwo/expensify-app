using Microsoft.AspNetCore.Authorization;
using Expensify.Common.Infrastructure.Authorization.Policies;
using Expensify.Modules.Expenses.Domain.Policies;

namespace Expensify.Modules.Expenses.Infrastructure.Policies;

internal sealed class ExpensesPolicyFactory : IPolicyFactory
{
    public string ModuleName => "Expenses";

    public Dictionary<string, Action<AuthorizationPolicyBuilder>> GetPolicies()
    {
        return new Dictionary<string, Action<AuthorizationPolicyBuilder>>
        {
            [ExpensePolicyConsts.ReadPolicy] = policy => policy.RequireClaim(ExpensePolicyConsts.ReadPolicy),
            [ExpensePolicyConsts.WritePolicy] = policy => policy.RequireClaim(ExpensePolicyConsts.WritePolicy),
            [ExpensePolicyConsts.DeletePolicy] = policy => policy.RequireClaim(ExpensePolicyConsts.DeletePolicy),
            [ExpensePolicyConsts.AdminReadPolicy] = policy => policy.RequireClaim(ExpensePolicyConsts.AdminReadPolicy)
        };
    }
}
