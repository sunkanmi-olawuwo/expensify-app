using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Users.Application.Admin.Command.DeleteUser;

public sealed record DeleteUserCommand(Guid Id) : ICommand;
