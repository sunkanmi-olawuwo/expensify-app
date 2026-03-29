using System.Text.RegularExpressions;
using Expensify.Common.Domain;
using Expensify.Modules.Investments.Domain.Categories;

namespace Expensify.Modules.Investments.Domain.Accounts;

public sealed class InvestmentAccount : Entity<Guid>, IAuditableEntity
{
    private static readonly Regex CurrencyRegex = new("^[A-Z]{3}$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private InvestmentAccount()
    {
    }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Provider { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal? InterestRate { get; private set; }

    public DateTimeOffset? MaturityDate { get; private set; }

    public decimal CurrentBalance { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public bool IsDeleted => DeletedAtUtc.HasValue;

    public static Result<InvestmentAccount> Create(
        Guid userId,
        string name,
        string? provider,
        Guid categoryId,
        string currency,
        decimal? interestRate,
        DateTimeOffset? maturityDate,
        decimal currentBalance,
        string? notes,
        string categorySlug,
        string userCurrency)
    {
        Result validationResult = Validate(name, currency, interestRate, maturityDate, currentBalance, categorySlug, userCurrency);
        if (validationResult.IsFailure)
        {
            return Result.Failure<InvestmentAccount>(validationResult.Error);
        }

        var investment = new InvestmentAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name.Trim(),
            Provider = NormalizeOptional(provider),
            CategoryId = categoryId,
            Currency = currency,
            InterestRate = NormalizeInterestRate(categorySlug, interestRate),
            MaturityDate = NormalizeMaturityDate(categorySlug, maturityDate),
            CurrentBalance = currentBalance,
            Notes = NormalizeOptional(notes)
        };

        investment.Raise(new InvestmentAccountCreatedDomainEvent(investment.Id));
        return investment;
    }

    public Result Update(
        string name,
        string? provider,
        Guid categoryId,
        string currency,
        decimal? interestRate,
        DateTimeOffset? maturityDate,
        decimal currentBalance,
        string? notes,
        string categorySlug,
        string userCurrency)
    {
        Result validationResult = Validate(name, currency, interestRate, maturityDate, currentBalance, categorySlug, userCurrency);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        Name = name.Trim();
        Provider = NormalizeOptional(provider);
        CategoryId = categoryId;
        Currency = currency;
        InterestRate = NormalizeInterestRate(categorySlug, interestRate);
        MaturityDate = NormalizeMaturityDate(categorySlug, maturityDate);
        CurrentBalance = currentBalance;
        Notes = NormalizeOptional(notes);

        Raise(new InvestmentAccountUpdatedDomainEvent(Id));
        return Result.Success();
    }

    public Result MarkDeleted(DateTime deletedAtUtc)
    {
        if (DeletedAtUtc.HasValue)
        {
            return Result.Failure(InvestmentAccountErrors.AlreadyDeleted(Id));
        }

        DeletedAtUtc = deletedAtUtc;
        Raise(new InvestmentAccountSoftDeletedDomainEvent(Id));
        return Result.Success();
    }

    public Result Restore()
    {
        if (!DeletedAtUtc.HasValue)
        {
            return Result.Failure(InvestmentAccountErrors.NotDeleted(Id));
        }

        DeletedAtUtc = null;
        Raise(new InvestmentAccountRestoredDomainEvent(Id));
        return Result.Success();
    }

    public void RaiseDeletedEvent()
    {
        Raise(new InvestmentAccountDeletedDomainEvent(Id));
    }

    private static Result Validate(
        string name,
        string currency,
        decimal? interestRate,
        DateTimeOffset? maturityDate,
        decimal currentBalance,
        string categorySlug,
        string userCurrency)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(InvestmentAccountErrors.NameRequired());
        }

        if (!CurrencyRegex.IsMatch(currency))
        {
            return Result.Failure(InvestmentAccountErrors.InvalidCurrency(currency));
        }

        if (!string.Equals(currency, userCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(InvestmentAccountErrors.CurrencyMismatch(userCurrency, currency));
        }

        if (currentBalance < 0)
        {
            return Result.Failure(InvestmentAccountErrors.InvalidCurrentBalance());
        }

        if (interestRate.HasValue && (interestRate.Value < 0 || interestRate.Value > 100))
        {
            return Result.Failure(InvestmentAccountErrors.InvalidInterestRate());
        }

        if (InvestmentCategoryRules.RequiresInterestRate(categorySlug) && interestRate is null)
        {
            return Result.Failure(InvestmentAccountErrors.InterestRateRequired());
        }

        if (InvestmentCategoryRules.RequiresMaturityDate(categorySlug) && maturityDate is null)
        {
            return Result.Failure(InvestmentAccountErrors.MaturityDateRequired());
        }

        return Result.Success();
    }

    private static decimal? NormalizeInterestRate(string categorySlug, decimal? interestRate) =>
        InvestmentCategoryRules.SupportsInterestRate(categorySlug) ? interestRate : null;

    private static DateTimeOffset? NormalizeMaturityDate(string categorySlug, DateTimeOffset? maturityDate) =>
        InvestmentCategoryRules.SupportsMaturityDate(categorySlug) ? maturityDate : null;

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
