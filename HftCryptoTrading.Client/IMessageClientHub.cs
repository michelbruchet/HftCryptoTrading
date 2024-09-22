using HftCryptoTrading.Shared;

namespace HftCryptoTrading.Client
{
    public interface IMessageClientHub<T> where T : class
    {
        event EventHandler<T> ClientMessageReceived;
        event EventHandler<Guid> DelayedNotificationReceived;
        event EventHandler<Guid> MessageDistributedReceived;

        Task<OperationResult> BroadcastEvent(Guid id, string @namespace, T message);
        Task StartAsync(string @namespace);
        Task StopAsync();
    }
}