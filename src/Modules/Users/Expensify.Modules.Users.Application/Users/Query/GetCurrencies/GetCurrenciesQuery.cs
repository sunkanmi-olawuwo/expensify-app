using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Users.Query.GetCurrencies;

public sealed record GetCurrenciesQuery(bool IncludeInactive = false) : IQuery<IReadOnlyCollection<CurrencyResponse>>;
