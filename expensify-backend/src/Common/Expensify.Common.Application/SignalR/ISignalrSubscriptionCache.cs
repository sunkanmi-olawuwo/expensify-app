namespace Expensify.Common.Application.SignalR;

public interface ISignalrSubscriptionCache
{
    Task SubscribeGroup(string userId, string groupName);
    Task UnsubscribeGroup(string userId, string groupName);
    Task UnsubscribeAllGroups(string userId);
    Task<IReadOnlyCollection<string>> GetSubscribedGroups(string userId);
}
