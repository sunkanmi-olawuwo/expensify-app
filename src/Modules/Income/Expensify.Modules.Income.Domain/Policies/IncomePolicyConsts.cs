namespace Expensify.Modules.Income.Domain.Policies;

public static class IncomePolicyConsts
{
    public const string ReadPolicy = "income:read";
    public const string WritePolicy = "income:write";
    public const string DeletePolicy = "income:delete";
    public const string AdminReadPolicy = "admin:income:read";
}
