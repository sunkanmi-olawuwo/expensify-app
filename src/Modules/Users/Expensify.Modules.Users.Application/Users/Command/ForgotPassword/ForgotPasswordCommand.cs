using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Users.Application.Users.Command.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
