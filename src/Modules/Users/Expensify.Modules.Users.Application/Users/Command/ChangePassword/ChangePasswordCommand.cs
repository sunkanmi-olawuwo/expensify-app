using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Users.Application.Users.Command.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : ICommand;
