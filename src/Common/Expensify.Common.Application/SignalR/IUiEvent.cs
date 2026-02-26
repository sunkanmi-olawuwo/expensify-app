using MediatR;

namespace Expensify.Common.Application.SignalR;

public interface IUiEvent : INotification
{
    string Method { get; }
}
