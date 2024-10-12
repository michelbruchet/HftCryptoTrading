using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Exchanges.Core.Exchange;

public interface IExchangeClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Specify the ExchangeName Name
    /// </summary>
    string ExchangeName { get; }

    event EventHandler<OrderUpdateEvent> OnOrderUpdated;
    event EventHandler<AccountPositionUpdateEvent> OnAccountPositionUpdated;
    event EventHandler<AccountBalanceUpdateEvent> OnAccountBalanceUpdated;

    /// <summary>
    /// Retrieve the list of symbols or products on the ExchangeName
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
    Task<List<BookPriceData>> GetBookPricesAsync(IEnumerable<string> symboles);
    Task RegisterPriceChangeHandlerAsync(PublishSymbolAnalysedSuccessFullyEvent notification);
    Task<List<OpenOrder>> GetOpenedOrders();
    Task<PlaceOrderResult> PlaceMarketOrder(PlaceOrder placeOrder);
    Task TrackUserStream();
    Task<AccountPosition> GetCurrentPositions();
    Task<List<AccountBalance>> GetCurrentAccountBalancesGroupedByBaseAsset();
}
