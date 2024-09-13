using HftCryptoTrading.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Client;

/// <summary>
/// Client to communication with message hub
/// </summary>
/// <typeparam name="T"></typeparam>
public class MessageClientHub<T>
    where T : class
{
    private readonly HubConnection _connection;
    private readonly HubClientPublisher _publisher;
    private readonly HubClientReceiver<T> _receiver;

    public event EventHandler<T> ClientMessageReceived;
    public event EventHandler<Guid> MessageDistributedReceived;
    public event EventHandler<Guid> DelayedNotificationReceived;

    /// <summary>
    /// Initialize a new instance with an url
    /// </summary>
    /// <param name="hubUrl"></param>
    public MessageClientHub(string hubUrl, string @namespace, string eventName)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl($"{hubUrl.TrimEnd('/')}/messages")
            .Build();

        _publisher = new HubClientPublisher(_connection, @namespace, eventName);
        _receiver = new HubClientReceiver<T>(_connection, @namespace, eventName);

        _receiver.ClientMessageReceived += (sender, message) => ClientMessageReceived?.Invoke(sender, message);
        _receiver.MessageDistributedReceived += (sender, id) => MessageDistributedReceived?.Invoke(sender, id);
        _receiver.DelayedNotificationReceived += (sender, id) => DelayedNotificationReceived?.Invoke(sender, id);
    }

    /// <summary>
    /// Starts the publisher and receiver and start listen for the event
    /// </summary>
    /// <returns></returns>
    public async Task StartAsync(string @namespace, string eventName)
    {
        await _receiver.StartAsync();
        await _connection.StartAsync();

        await _publisher.StartAsync(@namespace, eventName);
        Console.WriteLine("MessageClientHub connected to the hub.");
    }

    public async Task StopAsync()
    {
        await _publisher.StopAsync();
        await _receiver.StopAsync();
        await _connection.StopAsync();
        await _connection.DisposeAsync();
    }

    public async Task<OperationResult> BroadcastEvent<T>(Guid id, string @namespace, string eventName, T message)
    {
        return await _publisher.BroadcastEvent(id, @namespace, eventName, message);
    }
}
