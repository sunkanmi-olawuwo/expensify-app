using MediatR;
using Microsoft.AspNetCore.SignalR;
using Expensify.Common.Application.SignalR;
using Serilog;

namespace Expensify.Common.Infrastructure.SignalR;

public class SignalrNotificationHandler<TEvent>(IHubContext<ExpensifyHub> context, IEnumerable<IUiEventMapper<TEvent>> mappers, ILogger logger)
    : INotificationHandler<TEvent>
    where TEvent : class, IUiEvent
{
    public async Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        ILogger contextLogger = logger.ForContext<SignalrNotificationHandler<TEvent>>();

        contextLogger.Verbose("UI event {EventType} received.", typeof(TEvent).FullName);

        bool sentEvent = false;

        IEnumerable<SignalrEvent?> mappedEvents = mappers
            .Select(mapper => mapper.MapEvent(notification))
            .Where(mappedEvent => mappedEvent != null);

        foreach (SignalrEvent? mappedEvent in mappedEvents)
        {
            if (notification is IUiGroupEvent groupEvent)
            {
                await context.Clients.Group(groupEvent.GroupName).SendAsync(notification.Method, mappedEvent, cancellationToken);
                sentEvent = true;

                contextLogger
                    .ForContext("Event", mappedEvent, true)
                    .Debug("SignalR event {EventType} sent to group {GroupName}.", mappedEvent?.GetType().FullName, groupEvent.GroupName);
            }
            else
            {
                await context.Clients.All.SendAsync(notification.Method, mappedEvent, cancellationToken);
                sentEvent = true;

                contextLogger
                    .ForContext("Event", mappedEvent, true)
                    .Debug("SignalR event {EventType} sent to all.", mappedEvent?.GetType().FullName);
            }
        }

        if (!sentEvent)
        {
            contextLogger.Warning("No mappers found for event {EventType}. SignalR event not sent.", typeof(TEvent).FullName);
        }
    }
}
