using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Admin.Command.CreateCurrency;

public sealed record CreateCurrencyCommand(
    string Code,
    string Name,
    string Symbol,
    int MinorUnit,
    bool IsActive,
    bool IsDefault,
    int SortOrder) : ICommand<CurrencyResponse>;
