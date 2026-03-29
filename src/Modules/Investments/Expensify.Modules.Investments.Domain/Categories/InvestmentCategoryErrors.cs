using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Categories;

public static class InvestmentCategoryErrors
{
    private const string Prefix = "Investments.Categories";

    public static Error NotFound(Guid categoryId) =>
        Error.NotFound($"{Prefix}.NotFound", $"The investment category with identifier {categoryId} was not found");

    public static Error Inactive(Guid categoryId) =>
        Error.Validation($"{Prefix}.Inactive", $"The investment category with identifier {categoryId} is inactive");
}
