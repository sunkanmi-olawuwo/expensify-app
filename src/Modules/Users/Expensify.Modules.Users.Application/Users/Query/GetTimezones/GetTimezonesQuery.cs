using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Users.Query.GetTimezones;

public sealed record GetTimezonesQuery(bool IncludeInactive = false) : IQuery<IReadOnlyCollection<TimezoneResponse>>;
