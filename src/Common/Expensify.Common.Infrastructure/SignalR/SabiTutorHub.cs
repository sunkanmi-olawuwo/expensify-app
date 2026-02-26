using Microsoft.AspNetCore.SignalR;
using Expensify.Common.Application.SignalR;
using Serilog;

namespace Expensify.Common.Infrastructure.SignalR;

public class ExpensifyHub : Hub
{
    private readonly ILogger _logger;
    private readonly ISignalrSubscriptionCache _subscriptionCache;

    public ExpensifyHub(ILogger logger, ISignalrSubscriptionCache subscriptionCache)
    {
        _logger = logger.ForContext<ExpensifyHub>();
        _subscriptionCache = subscriptionCache;
    }

    private string UserIdFromContext => Context.UserIdentifier ?? "_anonymous";

    public async Task SubscribeGroup(string groupName)
    {
        _logger
            .ForContext("UserId", UserIdFromContext)
            .Debug("Subscribe to group {GroupName} received from connection {ConnectionId}.", groupName, Context.ConnectionId);
        await _subscriptionCache.SubscribeGroup(UserIdFromContext, groupName);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task UnsubscribeGroup(string groupName)
    {
        _logger
            .ForContext("UserId", UserIdFromContext)
            .Debug("Unsubscribe from group {GroupName} received from connection {ConnectionId}.", groupName, Context.ConnectionId);
        await _subscriptionCache.UnsubscribeGroup(UserIdFromContext, groupName);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        IReadOnlyCollection<string> groups = await _subscriptionCache.GetSubscribedGroups(UserIdFromContext);

        _logger
            .ForContext("UserId", UserIdFromContext)
            .ForContext("Groups", groups, true)
            .Debug("Connection {ConnectionId} connected. Found {GroupCount} groups to resubscribe the user to.", Context.ConnectionId, groups.Count);

        foreach (string group in groups)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger
            .ForContext("UserId", UserIdFromContext)
            .Debug(exception, "Connection {ConnectionId} disconnected.", Context.ConnectionId);
        if (exception is null)
        {
            _logger
                .ForContext("UserId", UserIdFromContext)
                .Verbose(exception, "No exception caused the disconnect, unsubscribing the user from all groups.");
            await _subscriptionCache.UnsubscribeAllGroups(UserIdFromContext);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
