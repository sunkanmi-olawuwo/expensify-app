using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Admin.Command.UpdateTimezone;

public sealed record UpdateTimezoneCommand(
    string IanaId,
    string DisplayName,
    bool IsActive,
    bool IsDefault,
    int SortOrder) : ICommand<TimezoneResponse>;
