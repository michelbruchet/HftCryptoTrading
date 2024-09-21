using HftCryptoTrading.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Exchanges.Core.Exchange;

public interface IExchangeClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Specify the Exchange Name
    /// </summary>
    string ExchangeName { get; }

    /// <summary>
    /// Retrieve the list of symbols or products on the Exchange
    /// </summary>
    /// <returns></returns>
    Task<List<SymbolData>> GetSymbolsAsync();

    /// <summary>
    /// Retrieve the list of tickers
    /// </summary>
    /// <returns></returns>
    Task<List<TickerData>> GetCurrentTickersAsync();

    /// <summary>
    /// Get for a specific symbol the list of klines
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    Task<List<KlineData>> GetHistoricalKlinesAsync(string symbol, TimeSpan period, DateTime startTime, DateTime endTime);

}
