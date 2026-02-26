using Expensify.Common.Application.Messaging;
using Expensify.Modules.Income.Application.Abstractions;
using Expensify.Modules.Income.Domain.Incomes;

namespace Expensify.Modules.Income.Application.Incomes.Command.UpdateIncome;

public sealed record UpdateIncomeCommand(
    Guid UserId,
    Guid IncomeId,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string? Source,
    IncomeType Type,
    string? Note) : ICommand<IncomeResponse>;
