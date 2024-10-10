using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HftCryptoTrading.Exchanges.Core.Exchange;

public class ExchangeProviderFactory(ILoggerFactory loggerFactory) : IExchangeProviderFactory
{
    private static ConcurrentDictionary<string, Func<AppSettings, ILoggerFactory, IExchangeClient>> _clients = new();

    public static void RegisterExchange(string name, ILoggerFactory loggerFactory, Func<AppSettings, ILoggerFactory,
        IExchangeClient> registrationFactory)
    {
        _clients.TryAdd(name, registrationFactory);
    }

    public IExchangeClient?
        GetExchange(string name, AppSettings settings, ILoggerFactory loggerFactory) =>
        _clients.TryGetValue(name, out var clientFactory) ?
            clientFactory.Invoke(settings, loggerFactory) : null;

    public IEnumerable<string> GetAllExchanges() => _clients.Keys;
}