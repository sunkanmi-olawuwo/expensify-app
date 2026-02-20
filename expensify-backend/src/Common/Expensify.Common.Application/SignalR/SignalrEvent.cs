namespace Expensify.Common.Application.SignalR;

public abstract record SignalrEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; private init; } = DateTime.UtcNow;

    protected SignalrEvent(Guid eventId, DateTime timestamp)
    {
        EventId = eventId;
        Timestamp = timestamp;
    }

    protected SignalrEvent(DateTime timestamp)
    {
        Timestamp = timestamp;
    }

    protected SignalrEvent() { }
}
