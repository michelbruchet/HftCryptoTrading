using HftCryptoTrading.Shared;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Services.Services;

public interface IHubClientManager
{
    Task AddToGroupAsync(string connectionId, string groupName);
    Task ReceiveMessage(string groupName, EventMessage message);
    Task ReceiveMessageDistributed(string senderConnectionId, string groupName, Guid messageId);
    Task ReceiveMessageDelayed(string connectionId, string groupName, Guid id);
    Task RemoveFromGroupAsync(string connectionId, string groupName);
}

public class MessageService(
    IHubClientManager hubContext, ILogger<MessageService> logger) : IMessageService
{
    private readonly ILogger<MessageService> _logger = logger;
    private const int _maxCacheDuration = 30;
    private readonly IHubClientManager _hubContext = hubContext;
    private readonly ConcurrentDictionary<string, int> _groups = new();
    private ConcurrentDictionary<string, List<(DateTime Timestamp, EventMessage Message, string SenderConnectionId)>> _unbroadcastedMessages 
        = new();

    public MessageService(
    IHubClientManager hubContext, ILogger<MessageService> logger,
    ConcurrentDictionary<string, List<(DateTime Timestamp, EventMessage Message, string SenderConnectionId)>> messages):
        this(hubContext, logger)
    {
        _unbroadcastedMessages = messages;
    }

    public List<(DateTime Timestamp, EventMessage Message, string SenderConnectionId)> Get(string groupName)
    {
        if (_unbroadcastedMessages.TryGetValue(groupName, out var list))
            return list;

        return null;
    }

    public async Task<OperationResult> 
        SubscribeAsync(string @namespace, string eventName, string connectionId)
    {
        CleanOldMessages(@namespace, eventName);

        if (string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(eventName))
        {
            _logger.LogWarning("Namespace or event name is null");
            return new FailedOperationResult("Namespace or event name is null");
        }

        string groupName = $"{@namespace}.{eventName}";

        await _hubContext.AddToGroupAsync(connectionId, groupName);
        _groups.AddOrUpdate(groupName, 1, (_, count) => count + 1);

        // Broadcast unbroadcasted messages if any exist
        if (_unbroadcastedMessages.TryRemove(groupName, out var messages))
        {
            foreach (var (timestamp, message, senderConnectionId) in messages)
            {
                await _hubContext.ReceiveMessage(groupName, message);
                await _hubContext.ReceiveMessageDistributed(senderConnectionId, groupName, message.Id);
            }
        }

        return new SuccessOperationResult();
    }

    public async Task<OperationResult> UnsubscribeAsync(
            string @namespace, string eventName, string connectionId
            )
    {
        if (string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(eventName))
        {
            _logger.LogWarning("Namespace or event name is null");
            return new FailedOperationResult("Namespace or event name is null");
        }

        string groupName = $"{@namespace}.{eventName}";
        await _hubContext.RemoveFromGroupAsync(connectionId, groupName);

        if (_groups.TryGetValue(groupName, out var count))
        {
            if (count <= 1)
            {
                _groups.TryRemove(groupName, out _);
            }
            else
            {
                _groups[groupName] = count - 1;
            }
        }

        return new SuccessOperationResult();
    }

    public async Task<OperationResult> 
        BroadcastEventAsync(EventMessage message, string connectionId)
    {
        if (message == null)
        {
            _logger.LogWarning("Message is null");
            return new FailedOperationResult("Message is null");
        }

        string groupName = $"{message.Namespace}.{message.EventName}";

        if (_groups.TryGetValue(groupName, out var count) && count > 1)
        {
            await _hubContext.ReceiveMessage(groupName, message);
            await _hubContext.ReceiveMessageDistributed(connectionId, groupName, message.Id);
        }
        else
        {
            _unbroadcastedMessages.AddOrUpdate(groupName,
                new List<(DateTime, EventMessage, string)> { (DateTime.UtcNow, message, connectionId) },
                (_, messages) =>
                {
                    messages.Add((DateTime.UtcNow, message, connectionId));
                    return messages;
                });

            await _hubContext.ReceiveMessageDelayed(connectionId, groupName, message.Id);
        }

        return new SuccessOperationResult();
    }

    public void CleanOldMessages(string @namespace, string eventName)
    {
        string groupName = $"{@namespace}.{eventName}";

        if (_unbroadcastedMessages.TryGetValue(groupName, out var messages))
        {
            messages.RemoveAll(m => DateTime.UtcNow.Subtract(m.Timestamp).TotalMinutes > _maxCacheDuration);
            if (!messages.Any())
            {
                _unbroadcastedMessages.TryRemove(groupName, out _);
            }
        }
    }

}
