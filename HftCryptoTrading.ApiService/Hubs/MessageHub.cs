using HftCryptoTrading.Shared;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace HftCryptoTrading.ApiService.Hubs;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Message hub used to broad cast event by namespace
/// </summary>
/// <param name="logger"></param>
public partial class MessageHub(ILogger<MessageHub> logger) : Hub<IClientMessageHub>, IMessageHub
{
    private const int _maxCacheDuration = 30;

    private static readonly ConcurrentDictionary<string, int> _groups = new();
    private static readonly ConcurrentDictionary<string, List<(DateTime Timestamp, EventMessage Message, string SenderConnectionId)>> _unbroadcastedMessages = new();

    private static readonly ActivitySource ActivitySource = new ActivitySource("MessageHub");

    [LoggerMessage(LogLevel.Information, "Subscribed to group: {GroupName}. Namespace: {Namespace}, EventName: {EventName}")]
    partial void OnSubscribeCompleted(string @namespace, string eventName, string groupName);

    [LoggerMessage(LogLevel.Information, "Unsubscribed from group: {GroupName}. Namespace: {Namespace}, EventName: {EventName}")]
    partial void OnUnsubscribeCompleted(string @namespace, string eventName, string groupName);

    [LoggerMessage(LogLevel.Information, "Broadcasted message to group: {GroupName}. Namespace: {Namespace}, EventName: {EventName}, Id: {Id}")]
    partial void OnBroadcastEventCompleted(Guid id, string @namespace, string eventName, string groupName);

    [LoggerMessage(LogLevel.Warning, "Unbroadcasted message to group: {GroupName}. Namespace: {Namespace}, EventName: {EventName}, Id: {Id}")]
    partial void OnUnBroadcastEventCompleted(Guid id, string @namespace, string eventName, string groupName);

    /// <summary>
    /// Subscribe to a new event channel
    /// </summary>
    public async Task<OperationResult> Subscribe(string @namespace, string eventName)
    {
        CleanOldMessages(@namespace, eventName);

        using var activity = ActivitySource.StartActivity("Subscribe");
        activity?.AddTag("namespace", @namespace);
        activity?.AddTag("eventName", eventName);

        if (string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(eventName))
        {
            logger.LogWarning("Namespace or event name is null");
            return new FailedOperationResult("Namespace or event name is null");
        }

        string groupName = $"{@namespace}.{eventName}";

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _groups.AddOrUpdate(groupName, 1, (_, count) => count + 1);

        // Broadcast unbroadcasted messages if any exist
        if (_unbroadcastedMessages.TryRemove(groupName, out var messages))
        {
            foreach (var (timestamp, message, senderConnectionId) in messages)
            {
                await Clients.Caller.ReceiveMessage(message);
                await Clients.Client(senderConnectionId).ReceiveMessageDistributed(groupName, message.Id);
            }
        }

        OnSubscribeCompleted(@namespace, eventName, groupName);
        return new SuccessOperationResult();
    }

    /// <summary>
    /// Unsubscribe from an existing event channel
    /// </summary>
    public async Task<OperationResult> Unsubscribe(string @namespace, string eventName)
    {
        using var activity = ActivitySource.StartActivity("Unsubscribe");
        activity?.AddTag("namespace", @namespace);
        activity?.AddTag("eventName", eventName);

        if (string.IsNullOrEmpty(@namespace) || string.IsNullOrEmpty(eventName))
        {
            logger.LogWarning("Namespace or event name is null");
            return new FailedOperationResult("Namespace or event name is null");
        }

        string groupName = $"{@namespace}.{eventName}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

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

        OnUnsubscribeCompleted(@namespace, eventName, groupName);
        return new SuccessOperationResult();
    }

    /// <summary>
    /// Broadcast a message to all clients subscribed to an event channel
    /// </summary>
    public async Task<OperationResult> BroadcastEvent(EventMessage message)
    {
        try
        {
            if (message == null)
            {
                logger.LogWarning("Message is null");
                return new FailedOperationResult("Message is null");
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(message);

            // Validate the message object
            if (!Validator.TryValidateObject(message, validationContext, validationResults, true))
            {
                var errorMessage = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
                logger.LogWarning("Message validation failed: {ErrorMessage}", errorMessage);
                return new FailedOperationResult(errorMessage);
            }

            CleanOldMessages(message.Namespace, message.EventName);

            string groupName = $"{message.Namespace}.{message.EventName}";

            if (_groups.TryGetValue(groupName, out var count) && count > 1)
            {
                using var activity = ActivitySource.StartActivity("BroadcastEvent");
                activity?.AddTag("namespace", message.Namespace);
                activity?.AddTag("eventName", message.EventName);
                activity?.AddTag("groupname", groupName);

                await Clients.OthersInGroup(groupName).ReceiveMessage(message);

                // Notifie le client qui a envoyé le message
                await Clients.Caller.ReceiveMessageDistributed(groupName, message.Id);
                OnBroadcastEventCompleted(message.Id, message.Namespace, message.EventName, groupName);
            }
            else
            {
                // Stocke le message non diffusé avec l'ID de connexion de l'expéditeur
                _unbroadcastedMessages.AddOrUpdate(groupName,
                    new List<(DateTime, EventMessage, string)> { (DateTime.UtcNow, message, Context.ConnectionId) },
                    (_, messages) =>
                    {
                        messages.Add((DateTime.UtcNow, message, Context.ConnectionId));
                        return messages;
                    });

                await Clients.Caller.ReceiveMessageDelayed(groupName, message.Id);
                OnUnBroadcastEventCompleted(message.Id, message.Namespace, message.EventName, groupName);
            }

            return new SuccessOperationResult();
        }
        catch (Exception ex)
        {
            return new FailedOperationResult(ex.Message);
        }
    }

    private static void CleanOldMessages(string @namespace, string eventName)
    {
        using var activityClean = ActivitySource.StartActivity("CleanOldMessage");
        activityClean?.AddTag("namespace", @namespace);
        activityClean?.AddTag("eventName", eventName);

        string groupName = $"{@namespace}.{eventName}";

        // Remove old messages
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
