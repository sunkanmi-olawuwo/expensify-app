using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Admin.Command.CreateTimezone;

public sealed record CreateTimezoneCommand(
    string IanaId,
    string DisplayName,
    bool IsActive,
    bool IsDefault,
    int SortOrder) : ICommand<TimezoneResponse>;
