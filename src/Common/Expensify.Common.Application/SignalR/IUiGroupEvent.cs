namespace Expensify.Common.Application.SignalR;

public interface IUiGroupEvent : IUiEvent
{
    string GroupName { get; }
}
