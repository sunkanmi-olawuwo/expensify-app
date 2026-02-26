using MediatR;
using Expensify.Common.Application.EventBus;
using Expensify.Common.Application.Exceptions;
using Expensify.Common.Application.Messaging;
using Expensify.Common.Domain;
using Expensify.Modules.Users.Application.Abstractions;
using Expensify.Modules.Users.Application.Admin.Query;
using Expensify.Modules.Users.Application.Users.Query.GetUser;
using Expensify.Modules.Users.Domain.Users;
using Expensify.Modules.Users.IntegrationEvents;

namespace Expensify.Modules.Users.Application.Users.Command.RegisterUser;

internal sealed class UserRegisteredDomainEventHandler(ISender sender, IEventBus bus)
    : DomainEventHandler<UserRegisteredDomainEvent>
{
    public override async Task Handle(
        UserRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        Result<GetUserResponse> result = await sender.Send(
            new GetUserQuery(domainEvent.UserId),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new ExpensifyException(nameof(GetUserQuery), result.Error);
        }

        await bus.PublishAsync(
            new UserRegisteredIntegrationEvent(
                domainEvent.Id,
                domainEvent.OccurredOnUtc,
                result.Value.Id,
                result.Value.FirstName,
                result.Value.LastName),
            cancellationToken);
    }
}
