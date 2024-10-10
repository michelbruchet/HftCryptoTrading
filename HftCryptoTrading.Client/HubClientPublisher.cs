using HftCryptoTrading.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace HftCryptoTrading.Client;

public class HubClientPublisher : IMessageHub
{
    private readonly HubConnection _connection;
    private string _namespace;
    private string? _eventName;

    public HubClientPublisher(HubConnection connection, string @namespace, string? eventName = null)
    {
        _connection = connection;
        _namespace = @namespace;
        _eventName = eventName;
    }

    public HubClientPublisher(string hubUrl, string @namespace, string eventName)
        :this(new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build(), @namespace, eventName)
    {
    }

    public HubClientPublisher(AppSettings appSetting, string? eventName = null):
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
        .Build(), appSetting.Hub.NameSpace, eventName)
    {
    }

    public HubClientPublisher(AppSettings appSetting) :
    this(appSetting, null)
    { }

    public async Task StartAsync(string @namespace)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(nameof(@namespace));
        ArgumentNullException.ThrowIfNullOrWhiteSpace(nameof(_eventName));

        _namespace = @namespace;

        Console.WriteLine("Publisher connected to the hub.");

        await _connection.StartAsync();
        await Subscribe(@namespace, _eventName);
    }

    public async Task StopAsync()
    {
        await Unsubscribe(_namespace, _eventName);
    }

    /// <summary>
    /// Broadcasts a strongly-typed event with data validation using ValidationContext.
    /// </summary>
    [Obsolete("This method is internal used and should not be used directly; please use BroadcastEvent<T> instead.", true)]
    public async Task<OperationResult> BroadcastEvent(EventMessage message)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Broadcasts a strongly-typed event with data validation using ValidationContext.
    /// Serializes the message and creates an EventMessage object.
    /// </summary>
    public async Task<OperationResult> BroadcastEvent<T>(Guid id, string @namespace, T message)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        ArgumentException.ThrowIfNullOrWhiteSpace(@namespace, nameof(@namespace));
        ArgumentException.ThrowIfNullOrWhiteSpace(_eventName, nameof(_eventName));

        if(id == Guid.Empty) throw new ArgumentNullException(nameof(id));

        Contract.EndContractBlock();

        var validationContext = new ValidationContext(message);

        var validationResults = new List<ValidationResult>();

        // Validate the message using DataAnnotations
        if (!Validator.TryValidateObject(message, validationContext, validationResults, true))
        {
            var errorMessage = string.Join(", ", validationResults.Select(vr => vr.ErrorMessage));
            return new FailedOperationResult(errorMessage);
        }

        // Create an EventMessage and set the serialized payload
        var eventMessage = new EventMessage(id, @namespace, _eventName);

        // Serialize the payload
        eventMessage.SetPayload(message);

        try
        {
            // Publish the event to the hub
            await _connection.InvokeAsync("BroadcastEvent", eventMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return new SuccessOperationResult();

    }

    /// <summary>
    /// Subscribes to a namespace and event.
    /// </summary>
    public async Task<OperationResult> Subscribe(string @namespace, string eventName)
    {
        await _connection.InvokeAsync("Subscribe", @namespace, eventName);
        return new SuccessOperationResult();
    }

    /// <summary>
    /// Unsubscribes from a namespace and event.
    /// </summary>
    public async Task<OperationResult> Unsubscribe(string @namespace, string eventName)
    {
        await _connection.InvokeAsync("Unsubscribe", @namespace, eventName);
        return new SuccessOperationResult();
    }
}
