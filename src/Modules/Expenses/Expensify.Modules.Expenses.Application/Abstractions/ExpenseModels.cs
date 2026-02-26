namespace Expensify.Modules.Expenses.Application.Abstractions;

public sealed record ExpenseResponse(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Currency,
    DateOnly Date,
    Guid CategoryId,
    string CategoryName,
    string Merchant,
    string Note,
    string PaymentMethod,
    IReadOnlyCollection<Guid> TagIds,
    IReadOnlyCollection<string> TagNames);

public sealed record ExpenseListItemResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    DateOnly Date,
    Guid CategoryId,
    string CategoryName,
    string Merchant,
    string Note,
    string PaymentMethod,
    IReadOnlyCollection<Guid> TagIds,
    IReadOnlyCollection<string> TagNames);

public sealed record ExpensesPageResponse(
    int Page,
    int PageSize,
    int TotalCount,
    int CurentPage,
    int TotalPages,
    IReadOnlyCollection<ExpenseListItemResponse> Items);

public sealed record MonthlyExpensesSummaryResponse(
    string Period,
    decimal TotalAmount,
    int ExpenseCount,
    IReadOnlyCollection<CategoryTotalResponse> Categories);

public sealed record CategoryTotalResponse(Guid CategoryId, string CategoryName, decimal Amount);

public sealed record ExpenseCategoryResponse(Guid Id, Guid UserId, string Name);

public sealed record ExpenseTagResponse(Guid Id, Guid UserId, string Name);
