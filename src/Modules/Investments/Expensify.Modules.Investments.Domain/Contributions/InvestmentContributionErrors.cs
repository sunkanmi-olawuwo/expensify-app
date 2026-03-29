using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Contributions;

public static class InvestmentContributionErrors
{
    private const string Prefix = "Investments.Contributions";

    public static Error InvalidAmount() =>
        Error.Validation($"{Prefix}.InvalidAmount", "Contribution amount must be greater than zero");

    public static Error AlreadyDeleted(Guid contributionId) =>
        Error.Problem($"{Prefix}.AlreadyDeleted", $"The contribution with identifier {contributionId} is already deleted");

    public static Error NotDeleted(Guid contributionId) =>
        Error.Problem($"{Prefix}.NotDeleted", $"The contribution with identifier {contributionId} is not deleted");
}
