using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Domain.Users;

namespace Expensify.Modules.Users.Application.Users.Command.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password, string FirstName, string LastName, RoleType Role )
    : ICommand<RegisterUserResponse>;
