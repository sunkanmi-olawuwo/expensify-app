namespace Expensify.Modules.Investments.Domain.Categories;

public static class InvestmentCategoryRules
{
    public static bool SupportsInterestRate(string slug) =>
        slug switch
        {
            InvestmentCategorySlugs.Isa => true,
            InvestmentCategorySlugs.Lisa => true,
            InvestmentCategorySlugs.FixedDeposit => true,
            InvestmentCategorySlugs.Other => true,
            _ => false
        };

    public static bool SupportsMaturityDate(string slug) =>
        slug switch
        {
            InvestmentCategorySlugs.FixedDeposit => true,
            InvestmentCategorySlugs.Other => true,
            _ => false
        };

    public static bool RequiresInterestRate(string slug) =>
        string.Equals(slug, InvestmentCategorySlugs.FixedDeposit, StringComparison.Ordinal);

    public static bool RequiresMaturityDate(string slug) =>
        string.Equals(slug, InvestmentCategorySlugs.FixedDeposit, StringComparison.Ordinal);
}
