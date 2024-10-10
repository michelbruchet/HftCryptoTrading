using HftCryptoTrading.Shared;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace HftCryptoTrading.ApiService.Hubs;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;
using HftCryptoTrading.Services.Services;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Message hub used to broad cast event by namespace
/// </summary>
/// <param name="logger"></param>
public partial class MessageHub : Hub<IClientMessageHub>, IMessageHub, IHubClientManager
{
    private const int _maxCacheDuration = 30;

    private static readonly ConcurrentDictionary<string, int> _groups = new();
    private static readonly ConcurrentDictionary<string, List<(DateTime Timestamp, EventMessage Message, string SenderConnectionId)>> _unbroadcastedMessages = new();
    private ILogger<MessageHub> _logger;
    private MessageService _messageService;

    public MessageHub(ILogger<MessageHub> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _messageService = new MessageService(this, loggerFactory.CreateLogger<MessageService>());
    }

    public async Task<OperationResult> Subscribe(string @namespace, string eventName)
    {
        return await _messageService.SubscribeAsync(@namespace, eventName, Context.ConnectionId);
    }

    public async Task<OperationResult> Unsubscribe(string @namespace, string eventName)
    {
        return await _messageService.UnsubscribeAsync(@namespace, eventName, Context.ConnectionId);
    }

    public async Task<OperationResult> BroadcastEvent(EventMessage message)
    {
        return await _messageService.BroadcastEventAsync(message, Context.ConnectionId);
    }

    public async Task AddToGroupAsync(string connectionId, string groupName)
    {
        await Groups.AddToGroupAsync(connectionId, groupName);
    }

    public async Task ReceiveMessage(string groupName, EventMessage message)
    {
        await Clients.OthersInGroup(groupName).ReceiveMessage(message);
    }

    public async Task ReceiveMessageDistributed(string senderConnectionId, string groupName, Guid messageId)
    {
        await Clients.Client(senderConnectionId).ReceiveMessageDistributed(groupName, messageId);
    }

    public async Task ReceiveMessageDelayed(string connectionId, string groupName, Guid id)
    {
        await Clients.Caller.ReceiveMessageDistributed(groupName, id);
    }

    public async Task RemoveFromGroupAsync(string connectionId, string groupName)
    {
        await Groups.RemoveFromGroupAsync(connectionId, groupName);
    }
}
