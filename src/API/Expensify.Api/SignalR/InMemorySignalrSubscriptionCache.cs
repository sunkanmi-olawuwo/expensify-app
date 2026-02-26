using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Expensify.Common.Application.SignalR;

namespace Expensify.Api.SignalR;

internal sealed class InMemorySignalrSubscriptionCache : ISignalrSubscriptionCache
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _cache = new();

    public Task SubscribeGroup(string userId, string groupName)
    {
        _cache.AddOrUpdate(userId, _ => [groupName], (_, groups) =>
        {
            groups.Add(groupName);
            return groups;
        });

        return Task.CompletedTask;
    }

    public Task UnsubscribeGroup(string userId, string groupName)
    {
        _cache.AddOrUpdate(userId, _ => [], (_, groups) =>
        {
            groups.Remove(groupName);
            return groups;
        });

        return Task.CompletedTask;
    }

    public Task UnsubscribeAllGroups(string userId)
    {
        _cache.TryRemove(userId, out _);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<string>> GetSubscribedGroups(string userId) =>
        Task.FromResult<IReadOnlyCollection<string>>(
            _cache.TryGetValue(userId, out HashSet<string>? groups)
                ? groups.ToArray()
                : ReadOnlyCollection<string>.Empty);
}
