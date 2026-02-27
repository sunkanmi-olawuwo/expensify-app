using Expensify.Common.Domain;

namespace Expensify.Modules.Income.Domain.Incomes;

public static class IncomeErrors
{
    private const string Prefix = "Income";

    public static Error NotFound(Guid incomeId) =>
        Error.NotFound($"{Prefix}.NotFound", $"The income with identifier {incomeId} was not found");

    public static Error InvalidAmount() =>
        Error.Validation($"{Prefix}.InvalidAmount", "Amount must be greater than zero");

    public static Error InvalidCurrency(string? currency) =>
        Error.Validation($"{Prefix}.InvalidCurrency", $"Currency '{currency}' must be a 3-letter uppercase ISO code");

    public static Error CurrencyMismatch(string expectedCurrency, string actualCurrency) =>
        Error.Validation($"{Prefix}.CurrencyMismatch", $"Income currency '{actualCurrency}' does not match user currency '{expectedCurrency}'");

    public static Error AlreadyDeleted(Guid incomeId) =>
        Error.Problem($"{Prefix}.AlreadyDeleted", $"The income with identifier {incomeId} is already deleted");

    public static Error NotDeleted(Guid incomeId) =>
        Error.Problem($"{Prefix}.NotDeleted", $"The income with identifier {incomeId} is not deleted");

    public static Error PeriodInvalid(string period) =>
        Error.Validation($"{Prefix}.InvalidPeriod", $"Period '{period}' must be in YYYY-MM format");
}
