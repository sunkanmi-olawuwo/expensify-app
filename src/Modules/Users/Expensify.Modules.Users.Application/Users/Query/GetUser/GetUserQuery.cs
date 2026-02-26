using Expensify.Common.Application.Messaging;
using Expensify.Modules.Users.Application.Abstractions;

namespace Expensify.Modules.Users.Application.Users.Query.GetUser;

public sealed record GetUserQuery(Guid Id) : IQuery<GetUserResponse>;
