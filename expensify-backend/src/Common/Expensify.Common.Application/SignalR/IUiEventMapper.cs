namespace Expensify.Common.Application.SignalR;

public interface IUiEventMapper<in TEvent> where TEvent : class, IUiEvent
{
    SignalrEvent? MapEvent(TEvent sourceEvent);
}
