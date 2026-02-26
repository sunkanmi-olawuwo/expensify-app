using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Expensify.Common.Infrastructure.Authorization.Policies;

public class AuthorizationConfigureOptions(
    IEnumerable<IPolicyFactory> policyFactories,
    ILogger<AuthorizationConfigureOptions> logger)
    : IConfigureOptions<AuthorizationOptions>
{
    public void Configure(AuthorizationOptions options)
    {
        foreach (IPolicyFactory factory in policyFactories)
        {
            logger.LogInformation("Configuring authorization policies for module: {ModuleName}", factory.ModuleName);

            Dictionary<string, Action<AuthorizationPolicyBuilder>> policies = factory.GetPolicies();

            foreach ((string? policyName, Action<AuthorizationPolicyBuilder>? policyBuilder) in policies)
            {
                options.AddPolicy(policyName, policyBuilder);
                logger.LogDebug("Added policy: {PolicyName}", policyName);
            }
        }
    }
}
