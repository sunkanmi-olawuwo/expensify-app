using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Users.Application.Users.Command.Logout;

public sealed record LogoutCommand(Guid UserId) : ICommand;
