using Microsoft.Extensions.Logging;

namespace HftCryptoTrading.Exchanges.Core.Exchange
{
    public interface IExchangeProviderFactory
    {
        IEnumerable<string> GetAllExchanges();
        IExchangeClient? GetExchange(string name, AppSettings settings, ILoggerFactory loggerFactory);
    }
}