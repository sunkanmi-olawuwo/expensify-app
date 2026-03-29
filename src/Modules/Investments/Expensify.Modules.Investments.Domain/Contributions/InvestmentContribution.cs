using Expensify.Common.Domain;

namespace Expensify.Modules.Investments.Domain.Contributions;

public sealed class InvestmentContribution : Entity<Guid>, IAuditableEntity
{
    private InvestmentContribution()
    {
    }

    public Guid InvestmentId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTimeOffset Date { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public bool IsDeleted => DeletedAtUtc.HasValue;

    public static Result<InvestmentContribution> Create(Guid investmentId, decimal amount, DateTimeOffset date, string? notes)
    {
        if (amount <= 0)
        {
            return Result.Failure<InvestmentContribution>(InvestmentContributionErrors.InvalidAmount());
        }

        var contribution = new InvestmentContribution
        {
            Id = Guid.NewGuid(),
            InvestmentId = investmentId,
            Amount = amount,
            Date = date,
            Notes = NormalizeOptional(notes)
        };

        contribution.Raise(new InvestmentContributionCreatedDomainEvent(contribution.Id));
        return contribution;
    }

    public Result MarkDeleted(DateTime deletedAtUtc)
    {
        if (DeletedAtUtc.HasValue)
        {
            return Result.Failure(InvestmentContributionErrors.AlreadyDeleted(Id));
        }

        DeletedAtUtc = deletedAtUtc;
        Raise(new InvestmentContributionSoftDeletedDomainEvent(Id));
        return Result.Success();
    }

    public Result Restore()
    {
        if (!DeletedAtUtc.HasValue)
        {
            return Result.Failure(InvestmentContributionErrors.NotDeleted(Id));
        }

        DeletedAtUtc = null;
        Raise(new InvestmentContributionRestoredDomainEvent(Id));
        return Result.Success();
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
