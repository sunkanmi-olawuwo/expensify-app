namespace Expensify.Modules.Investments.Domain.Policies;

public static class InvestmentPolicyConsts
{
    public const string ClaimEnabledValue = "true";
    public const string ReadPolicy = "investments:read";
    public const string WritePolicy = "investments:write";
    public const string DeletePolicy = "investments:delete";
    public const string AdminReadPolicy = "admin:investments:read";
    public const string AdminManageCategoriesPolicy = "admin:investments:manage-categories";
}
