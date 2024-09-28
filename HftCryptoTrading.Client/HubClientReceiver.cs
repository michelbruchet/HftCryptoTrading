using HftCryptoTrading.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Client;

public class HubClientReceiver<T> : IClientMessageHub
    where T : class
{
    public event EventHandler<T> ClientMessageReceived;
    public event EventHandler<Guid> MessageDistributedReceived;
    public event EventHandler<Guid> DelayedNotificationReceived;

    private readonly HubConnection _connection;
    private HubConnection connection;
    private string _namespace;
    private string _eventName;
    private string _groupName;

    public HubClientReceiver(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

    }

    public HubClientReceiver(HubConnection connection, string @namespace, string eventName)
    {
        _connection = connection;

        _namespace = @namespace;
        _eventName = eventName;
        _groupName = $"{_namespace}.{_eventName}";
    }

    public HubClientReceiver(AppSettings appSetting, string? eventName = null):
        this(new HubConnectionBuilder()
        .WithUrl($"{appSetting.Hub.HubApiUrl.TrimEnd('/')}/messages", options =>
        {
            options.Headers.Add("x-Api-Key", appSetting.Hub.HubApiKey);
            options.Headers.Add("x-Api-Secret", appSetting.Hub.HubApiSecret);
        })
        .WithAutomaticReconnect(new[]
        {
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        })
        .Build(), appSetting.Hub.NameSpace, eventName ?? typeof(T).Name)
    {

    }

    public async Task StartAsync()
    {
        // Set up the ReceiveMessage handler
        _connection.On<EventMessage>("ReceiveMessage", async (message) => {
            await ReceiveMessage(message);
            });

        _connection.On<string, Guid>("ReceiveMessageDistributed", 
            async (groupName, id) =>
            {
                if(groupName.Equals(_groupName, StringComparison.OrdinalIgnoreCase))
                    await ReceiveMessageDistributed(groupName, id);
            });

        _connection.On<string, Guid>("ReceiveMessageDelayed", 
            async (groupName, id) =>
            {
                if (groupName.Equals(_groupName, StringComparison.OrdinalIgnoreCase))
                    await ReceiveMessageDelayed(groupName, id);
            });

        await _connection.StartAsync();
        await _connection.InvokeAsync("Subscribe", _namespace, _eventName);

        Console.WriteLine("Receiver connected to the hub.");
    }

    public async Task StopAsync()
    {
    }

    /// <summary>
    /// Receives a strongly-typed event and validates it using ValidationContext.
    /// </summary>
    public async Task ReceiveMessage(EventMessage message)
    {
        var validationContext = new ValidationContext(message);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(message, validationContext, validationResults, true))
        {
            var errorMessage = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
            Console.WriteLine($"Validation failed: {errorMessage}");
            return;
        }

        var payLoad = message.GetPayload<T>();

        var vcPayLoad = new ValidationContext(payLoad);
        var vrPayLoad = new List<ValidationResult>();
        try
        {
            if (!Validator.TryValidateObject(payLoad, vcPayLoad, vrPayLoad, true))
            {
                var errorMessage = string.Join(", ", vrPayLoad.Select(vr => vr.ErrorMessage));
                Console.WriteLine($"Validation failed: {errorMessage}");
                return;
            }

            ClientMessageReceived?.Invoke(this, payLoad);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task ReceiveMessageDelayed(string groupName, Guid id)
    {
        DelayedNotificationReceived?.Invoke(this, id);
    }

    public async Task ReceiveMessageDistributed(string groupName, Guid id)
    {
        MessageDistributedReceived?.Invoke(this, id);
    }
}
