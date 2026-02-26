namespace Expensify.Modules.Income.Application.Abstractions;

public sealed record IncomeResponse(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string Source,
    string Type,
    string Note);

public sealed record IncomeListItemResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string Source,
    string Type,
    string Note);

public sealed record IncomePageResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int CurentPage,
    int TotalPages,
    IReadOnlyCollection<IncomeListItemResponse> Items);

public sealed record MonthlyIncomeSummaryResponse(
    string Period,
    decimal TotalAmount,
    int IncomeCount,
    IReadOnlyCollection<IncomeTypeTotalResponse> Types);

public sealed record IncomeTypeTotalResponse(string Type, decimal Amount);
