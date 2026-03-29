using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Accounts;

public static class InvestmentAccountErrors
{
    private const string Prefix = "Investments.Accounts";

    public static Error NameRequired() =>
        Error.Validation($"{Prefix}.NameRequired", "Investment account name is required");

    public static Error NotFound(Guid investmentId) =>
        Error.NotFound($"{Prefix}.NotFound", $"The investment account with identifier {investmentId} was not found");

    public static Error InvalidCurrentBalance() =>
        Error.Validation($"{Prefix}.InvalidCurrentBalance", "Current balance must be zero or greater");

    public static Error InvalidCurrency(string? currency) =>
        Error.Validation($"{Prefix}.InvalidCurrency", $"Currency '{currency}' must be a 3-letter uppercase ISO code");

    public static Error CurrencyMismatch(string expectedCurrency, string actualCurrency) =>
        Error.Validation($"{Prefix}.CurrencyMismatch", $"Investment currency '{actualCurrency}' does not match user currency '{expectedCurrency}'");

    public static Error InvalidInterestRate() =>
        Error.Validation($"{Prefix}.InvalidInterestRate", "Interest rate must be between 0 and 100");

    public static Error InterestRateRequired() =>
        Error.Validation($"{Prefix}.InterestRateRequired", "Interest rate is required for fixed deposit accounts");

    public static Error MaturityDateRequired() =>
        Error.Validation($"{Prefix}.MaturityDateRequired", "Maturity date is required for fixed deposit accounts");

    public static Error AlreadyDeleted(Guid investmentId) =>
        Error.Problem($"{Prefix}.AlreadyDeleted", $"The investment account with identifier {investmentId} is already deleted");

    public static Error NotDeleted(Guid investmentId) =>
        Error.Problem($"{Prefix}.NotDeleted", $"The investment account with identifier {investmentId} is not deleted");
}
