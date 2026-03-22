using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Users.Application.Users.Command.ResetPassword;

public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : ICommand;
