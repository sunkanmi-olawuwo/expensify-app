namespace Expensify.Modules.Investments.Application.Abstractions;

public sealed record InvestmentCategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive);

public sealed record InvestmentAccountListItemResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Provider,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    string Currency,
    decimal? InterestRate,
    DateTimeOffset? MaturityDate,
    decimal CurrentBalance,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record InvestmentAccountResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Provider,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    string Currency,
    decimal? InterestRate,
    DateTimeOffset? MaturityDate,
    decimal CurrentBalance,
    string? Notes,
    decimal TotalContributed,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record InvestmentAccountsPageResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<InvestmentAccountListItemResponse> Items);

public sealed record InvestmentContributionResponse(
    Guid Id,
    Guid InvestmentId,
    decimal Amount,
    DateTimeOffset Date,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record InvestmentContributionsPageResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyCollection<InvestmentContributionResponse> Items);

public sealed record PortfolioSummaryResponse(
    decimal TotalContributed,
    decimal CurrentValue,
    decimal TotalGainLoss,
    decimal GainLossPercentage,
    int AccountCount,
    string Currency);
