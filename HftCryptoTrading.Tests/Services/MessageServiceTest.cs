using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using HftCryptoTrading.Services.Services;
using System.Collections.Concurrent;
using HftCryptoTrading.Shared;

namespace HftCryptoTrading.Tests.Services;

public class MessageServiceTests
{
    private readonly Mock<IHubClientManager> _mockHubContext;
    private readonly Mock<ILogger<MessageService>> _mockLogger;

    public MessageServiceTests()
    {
        _mockHubContext = new Mock<IHubClientManager>();
        _mockLogger = new Mock<ILogger<MessageService>>();
    }

    [Fact]
    public async Task SubscribeAsync_WithNullNamespaceOrEventName_ShouldReturnFailedResult()
    {
        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object);

        // Act
        var result = await _messageService.SubscribeAsync(null, "event", "connectionId");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SubscribeAsync_ValidData_ShouldAddToGroupAndReturnSuccess()
    {
        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object);

        // Arrange
        var namespaceValue = "namespace";
        var eventName = "event";
        var connectionId = "connectionId";

        // Act
        var result = await _messageService.SubscribeAsync(namespaceValue, eventName, connectionId);

        // Assert
        Assert.True(result.IsSuccess);
        string expectedGroupName = $"{namespaceValue}.{eventName}";
        _mockHubContext.Verify(h => h.AddToGroupAsync(connectionId, expectedGroupName), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeAsync_WithNullNamespaceOrEventName_ShouldReturnFailedResult()
    {
        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object);

        // Act
        var result = await _messageService.UnsubscribeAsync(null, "event", "connectionId");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UnsubscribeAsync_ValidData_ShouldRemoveFromGroupAndReturnSuccess()
    {
        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object);

        // Arrange
        var namespaceValue = "namespace";
        var eventName = "event";
        var connectionId = "connectionId";

        await _messageService.SubscribeAsync(namespaceValue, eventName, connectionId);

        // Act
        var result = await _messageService.UnsubscribeAsync(namespaceValue, eventName, connectionId);

        // Assert
        Assert.True(result.IsSuccess);
        string expectedGroupName = $"{namespaceValue}.{eventName}";
        _mockHubContext.Verify(h => h.RemoveFromGroupAsync(connectionId, expectedGroupName), Times.Once);
    }

    [Fact]
    public async Task BroadcastEventAsync_WithNullMessage_ShouldReturnFailedResult()
    {
        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object);

        // Act
        var result = await _messageService.BroadcastEventAsync(null, "connectionId");

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task BroadcastEventAsync_WithValidMessage_ShouldBroadcastWhenGroupHasMoreThanOneSubscriber()
    {
        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object);

        // Arrange
        var message = new EventMessage { Namespace = "namespace", EventName = "event", Id = Guid.NewGuid() };
        var connectionId = "connectionId";
        await _messageService.SubscribeAsync(message.Namespace, message.EventName, "FirstConnection");
        await _messageService.SubscribeAsync(message.Namespace, message.EventName, "anotherConnection");

        // Act
        var result = await _messageService.BroadcastEventAsync(message, connectionId);

        // Assert
        Assert.True(result.IsSuccess);
        string expectedGroupName = $"{message.Namespace}.{message.EventName}";
        _mockHubContext.Verify(h => h.ReceiveMessage(expectedGroupName, message), Times.Once);
        _mockHubContext.Verify(h => h.ReceiveMessageDistributed(connectionId, expectedGroupName, message.Id), Times.Once);
    }

    [Fact]
    public async Task BroadcastEventAsync_WithSingleSubscriber_ShouldDelayMessage()
    {
        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object);

        // Arrange
        var message = new EventMessage { Namespace = "namespace", EventName = "event", Id = Guid.NewGuid() };
        var connectionId = "connectionId";

        // Act
        var result = await _messageService.BroadcastEventAsync(message, connectionId);

        // Assert
        Assert.True(result.IsSuccess);
        string expectedGroupName = $"{message.Namespace}.{message.EventName}";
        _mockHubContext.Verify(h => h.ReceiveMessageDelayed(connectionId, expectedGroupName, message.Id), Times.Once);
    }

    [Fact]
    public void CleanOldMessages_ShouldRemoveExpiredMessages()
    {
        // Arrange
        var groupName = "namespace.event";
        var oldMessageTimestamp = DateTime.UtcNow.AddMinutes(-31);
        var recentMessageTimestamp = DateTime.UtcNow.AddMinutes(-1);

        var dicto = new ConcurrentDictionary<string, List<(DateTime Timestamp, EventMessage Message, string SenderConnectionId)>>();

        dicto.TryAdd(groupName, new List<(DateTime Timestamp, EventMessage Message, string SenderConnectionId)>{
            { (oldMessageTimestamp, new EventMessage(), Guid.NewGuid().ToString()) }
        });

        var _messageService = new MessageService(_mockHubContext.Object, _mockLogger.Object, dicto);

        // Act
        _messageService.CleanOldMessages("namespace", "event");

        // Assert
        Assert.Null(_messageService.Get(groupName));
    }
}
