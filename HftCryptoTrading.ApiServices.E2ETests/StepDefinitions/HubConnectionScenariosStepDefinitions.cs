using System;
using Aspire.Hosting.Testing;
using HftCryptoTrading.Client;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Reqnroll;

namespace HftCryptoTrading.ApiServices.E2ETests.StepDefinitions;

[Binding, Scope(Feature = "Hub Connection Scenarios")]
public partial class HubConnectionScenariosStepDefinitions
{
    private readonly AspireHostHook _hook;
    private readonly Uri _endPoint;
    private readonly string _wssUri;
    private string _receivedMessage;

    private MessageClientHub<MockData> _client1;

    private Guid? _message1Id;
    private Guid? _MessageDistributedReceived1Id;
    private MockData? _ClientMessageReceived1Id;
    private Guid? _DelayedNotificationReceived1Id;

    private AutoResetEvent _waitMessageDistributedReceived1Id;
    private AutoResetEvent _waitClientMessageReceived1Id;
    private AutoResetEvent _waitDelayedNotificationReceived1Id;


    private MessageClientHub<MockData> _client2;

    private Guid? _message2Id;
    private Guid? _MessageDistributedReceived2Id;
    private MockData? _ClientMessageReceived2Id;
    private Guid? _DelayedNotificationReceived2Id;

    private AutoResetEvent _waitMessageDistributedReceived2Id;
    private AutoResetEvent _waitClientMessageReceived2Id;
    private AutoResetEvent _waitDelayedNotificationReceived2Id;

    public HubConnectionScenariosStepDefinitions(AspireHostHook hook)
    {
        _hook = hook;
        _endPoint = _hook.App.GetEndpoint("apiservice");
        _wssUri = _endPoint.AbsoluteUri.ToString()
            .Replace("http://", "ws://")
            .Replace("https://", "wss://");

        _waitClientMessageReceived1Id = new AutoResetEvent(false);
        _waitDelayedNotificationReceived1Id = new(false);
        _waitMessageDistributedReceived1Id = new(false);

        _waitClientMessageReceived2Id = new AutoResetEvent(false);
        _waitDelayedNotificationReceived2Id = new(false);
        _waitMessageDistributedReceived2Id = new(false);
    }

    [Given("a client is connected for the namespace {string} and event {string}")]
    public async Task GivenAClientIsConnectedForTheNamespaceAndEvent(string @namespace, string @event)
    {
        _client1 = new MessageClientHub<MockData>(_wssUri, @namespace, @event);
        _client1.DelayedNotificationReceived += _client1_DelayedNotificationReceived;
        _client1.ClientMessageReceived += _client1_ClientMessageReceived;
        _client1.MessageDistributedReceived += _client1_MessageDistributedReceived;

        await _client1.StartAsync(@namespace, @event);
    }

    private void _client1_MessageDistributedReceived(object? sender, Guid e)
    {
        _MessageDistributedReceived1Id = e;
        _waitMessageDistributedReceived1Id?.Set();
    }

    private void _client1_ClientMessageReceived(object? sender, MockData e)
    {
        _ClientMessageReceived1Id = e;
        _waitClientMessageReceived1Id?.Set();
    }

    private void _client1_DelayedNotificationReceived(object? sender, Guid e)
    {
        _DelayedNotificationReceived1Id = e;
        _waitDelayedNotificationReceived1Id?.Set();
    }

    [When("the client publishes a message {string} to namespace {string} and event {string}")]
    public async Task WhenTheClientPublishesAMessageToNamespaceAndEvent(string id, string @namespace, string @event)
    {
        _message1Id = Guid.Parse(id);

        var data = new MockData(_message1Id.Value, DateTime.UtcNow);
        await _client1.BroadcastEvent<MockData>(_message1Id.Value, @namespace, @event, data);
    }

    [Then("the client should receive a delayed notification")]
    public void ThenTheClientShouldReceiveADelayedNotificationWithTheMessage()
    {
        _waitDelayedNotificationReceived1Id.WaitOne();
        Assert.IsNotNull(_DelayedNotificationReceived1Id);
        Assert.AreEqual(_DelayedNotificationReceived1Id, _message1Id);
    }

    [When("another client subscribes for the namespace {string} and event {string}")]
    public async Task WhenAnotherClientSubscribesForTheNamespaceAndEvent(string @namespace, string @event)
    {
        _client2 = new MessageClientHub<MockData>(_wssUri, @namespace, @event);

        _client2.DelayedNotificationReceived += _client2_DelayedNotificationReceived;
        _client2.ClientMessageReceived += _client2_ClientMessageReceived;
        _client2.MessageDistributedReceived += _client2_MessageDistributedReceived;

        await _client2.StartAsync(@namespace, @event);
    }

    private void _client2_MessageDistributedReceived(object? sender, Guid e)
    {
        _MessageDistributedReceived2Id = e;
        _waitMessageDistributedReceived2Id?.Set();
    }

    private void _client2_ClientMessageReceived(object? sender, MockData e)
    {
        _ClientMessageReceived2Id = e;
        _waitClientMessageReceived2Id?.Set();
    }

    private void _client2_DelayedNotificationReceived(object? sender, Guid e)
    {
        _DelayedNotificationReceived2Id = e;
        _waitDelayedNotificationReceived2Id?.Set();
    }

    [Then("the second client should receive the pending message")]
    public void ThenTheSecondClientShouldReceiveThePendingMessage()
    {
        //Second client should receive the message delayed for the first client
        Assert.IsNotNull(_waitClientMessageReceived2Id);
        _waitClientMessageReceived2Id?.WaitOne();
        Assert.IsNotNull(_ClientMessageReceived2Id);
        Assert.AreEqual(_ClientMessageReceived2Id.Id, _message1Id);
    }

    [Then("the first client should receive a message distribution notification")]
    public void ThenTheFirstClientShouldReceiveAMessageDistributionNotification()
    {
        //first client should received a delay message notification
        _waitMessageDistributedReceived1Id?.WaitOne();
        Assert.IsNotNull(_MessageDistributedReceived1Id);
    }
}
