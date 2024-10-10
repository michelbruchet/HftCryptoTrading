namespace HftCryptoTrading.Shared;

public interface IClientMessageHub
{
    Task ReceiveMessage(EventMessage message);
    Task ReceiveMessageDelayed(string groupName, Guid id);  // Notify about delayed message
    Task ReceiveMessageDistributed(string groupName, Guid id);  // Notify about message distribution
}

public interface IMessageHub
{
    Task<OperationResult> Subscribe(string @namespace, string eventName);
    Task<OperationResult> Unsubscribe(string @namespace, string eventName);
    Task<OperationResult> BroadcastEvent(EventMessage message);
}
