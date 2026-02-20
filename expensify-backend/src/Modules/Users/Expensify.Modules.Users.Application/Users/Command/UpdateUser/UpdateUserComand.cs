using Expensify.Common.Application.Messaging;

namespace Expensify.Modules.Users.Application.Users.Command.UpdateUser;

public sealed record UpdateUserCommand(Guid UserId, string FirstName, string LastName) : ICommand;
