using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Users.Command.Login;

public sealed record LoginCommand(string Email, string Password)
    : ICommand<LoginUserResponse>;
