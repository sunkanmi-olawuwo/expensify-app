using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Admin.Command.UpdateCurrency;

public sealed record UpdateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    int MinorUnit,
    bool IsActive,
    bool IsDefault,
    int SortOrder) : ICommand<CurrencyResponse>;
