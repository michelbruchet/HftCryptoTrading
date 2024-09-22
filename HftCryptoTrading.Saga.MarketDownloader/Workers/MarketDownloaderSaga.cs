using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.MarketDownloader.Processes;
using HftCryptoTrading.Saga.MarketDownloader.Workers;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared;
using MediatR;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;
using HftCryptoTrading.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System.Data.Common;
using HftCryptoTrading.Shared.Events;

public class MarketDownloaderSaga : IMarketDownloaderSaga
{
    private readonly AppSettings _appSetting;
    private readonly IMediator _mediator;
    private readonly ExchangeProviderFactory _factory;
    private readonly IMetricService _metricService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HubConnection _hubConnection;
    private readonly List<Task> _downloadTasks = new(); // List to store ongoing tasks
    private readonly CancellationTokenSource _cancellationTokenSource = new(); // Cancellation source to stop tasks
    private readonly TimeSpan _delayBetweenDownloads = TimeSpan.FromHours(5); // Interval of 5 hours between downloads
    private readonly HubClientPublisher _publisher;

    public MarketDownloaderSaga(IOptions<AppSettings> appSetting, IMediator mediator, ILoggerFactory loggerFactory, ExchangeProviderFactory factory, IMetricService metricService)
    {
        _appSetting = appSetting.Value;
        _mediator = mediator;
        _factory = factory;
        _metricService = metricService;
        _loggerFactory = loggerFactory;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_appSetting.Hub.HubApiUrl.TrimEnd('/')}/messages", options =>
            {
                options.Headers.Add("x-Api-Key", _appSetting.Hub.HubApiKey);
                options.Headers.Add("x-Api-Secret", _appSetting.Hub.HubApiSecret);
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30)
            })
            .Build();

        _publisher = new HubClientPublisher(_hubConnection, _appSetting.Hub.NameSpace, typeof(SymbolAnalysePriceEvent).Name);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //Connect to the hub
        await _publisher.StartAsync(_appSetting.Hub.NameSpace);

        // Combine internal and external cancellation tokens
        using var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

        // Retrieve all registered exchanges
        var exchangeNames = _factory.GetAllExchanges();

        // Start a background DownloadWorkerProcess for each exchange
        foreach (var exchangeName in exchangeNames)
        {
            var exchangeClient = _factory.GetExchange(exchangeName, _appSetting, _loggerFactory);

            if (exchangeClient != null)
            {
                // Start the background task and add it to the task list
                var downloadTask = Task.Run(async () =>
                {
                    try
                    {
                        var workerProcess = new DownloadWorkerProcess(exchangeClient, _metricService, this, _appSetting);

                        // Periodic execution loop every 5 hours
                        while (!linkedCancellationToken.Token.IsCancellationRequested)
                        {
                            // Execute the download process
                            await workerProcess.DownloadSymbols();

                            // Wait for 5 hours or cancel
                            try
                            {
                                await Task.Delay(_delayBetweenDownloads, linkedCancellationToken.Token);
                            }
                            catch (TaskCanceledException)
                            {
                                // Handle cancellation
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle errors
                        Console.WriteLine($"Error in download process for {exchangeName}: {ex.Message}");
                    }
                }, linkedCancellationToken.Token);

                _downloadTasks.Add(downloadTask); // Add the task to the list
            }
        }

        // Wait for all tasks to finish if cancellationToken is canceled
        await Task.WhenAll(_downloadTasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Cancel all background tasks
        _cancellationTokenSource.Cancel();

        // Wait for all tasks to complete
        try
        {
            await Task.WhenAll(_downloadTasks);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Download processes were canceled.");
        }
    }

    public async Task PublishSymbols(string exchangeName, IEnumerable<SymbolTickerData> data)
    {
        await _mediator.Publish(new NewSymbolTickerDataEvent(exchangeName, data));
    }

    public Task PublishAbnormalPriceSymbols(string exchangeName, List<SymbolTickerData> abnormalPriceSymbols)
    {
        //TODO V2 : Manage abnormal price symbols
        return Task.CompletedTask;
    }

    public Task PublishAbnormalSpreadSymbols(string exchangeName, List<SymbolTickerData> abnormalSpreadSymbols)
    {
        //TODO V2 : Manage abnormal spread symbols
        return Task.CompletedTask;
    }

    public Task PublishAbnormalVolumeSymbols(string exchangeName, List<SymbolTickerData> abnormalVolumeSymbols)
    {
        //TODO V2 : Manage abnormal volume symbols
        return Task.CompletedTask;
    }

    public async Task PublishAnalyseMarketDoneSuccessFully(string exchange, List<SymbolTickerData> validSymbols)
    {
        await _mediator.Publish(new AnalyseMarketDoneSuccessFullyEvent(exchange, validSymbols));
    }

    public async Task PublishAnalysePriceDoneSuccessFully(string exchangeName, SymbolTickerData symbol)
    {
        await _publisher.BroadcastEvent<SymbolAnalysePriceEvent>(Guid.NewGuid(), _appSetting.Hub.NameSpace,
            new SymbolAnalysePriceEvent(exchangeName, symbol.Symbol.Symbol)
            {
                Ask = symbol.BookPrice?.BestAskPrice,
                Bid = symbol.BookPrice?.BestBidPrice,
                BestAskPrice = symbol.BookPrice?.BestAskPrice,
                BestAskQuantity = symbol.BookPrice?.BestAskQuantity,
                BestBidPrice = symbol.BookPrice?.BestBidPrice,
                BestBidQuantity = symbol.BookPrice?.BestBidQuantity,
                HighPrice = symbol.Ticker.HighPrice,
                LowPrice = symbol.Ticker.LowPrice,
                Price = symbol.Ticker.Price,
                Price24H = symbol.Ticker.Price24H,
                PriceChange = symbol.Ticker.PriceChange,
                PriceChangePercent = symbol.Ticker.PriceChangePercent,
                PublishedDate = DateTime.UtcNow,
                Volume = symbol.Ticker.Volume,
                Symbol = new Symbol
                {
                    Name = symbol.Symbol.Symbol,
                    AllowTrailingStop = symbol.Symbol.AllowTrailingStop,
                    ApplyToMarketOrders = symbol.Symbol.MinNotionalFilter?.ApplyToMarketOrders,
                    AveragePriceMinutes = symbol.Symbol.MinNotionalFilter?.AveragePriceMinutes,
                    BaseAsset = symbol.Symbol.BaseAsset,
                    BaseAssetPrecision = symbol.Symbol.BaseAssetPrecision,
                    BaseFeePrecision = symbol.Symbol.BaseFeePrecision,
                    CancelReplaceAllowed = symbol.Symbol.CancelReplaceAllowed,
                    IcebergAllowed = symbol.Symbol.IcebergAllowed,
                    IsMarginTradingAllowed = symbol.Symbol.IsMarginTradingAllowed,
                    IsSpotTradingAllowed = symbol.Symbol.IsSpotTradingAllowed,
                    IceBergPartsLimit = symbol.Symbol.IceBergPartsFilter?.Limit,
                    MaxNumberAlgorithmicOrders = symbol.Symbol.MaxAlgorithmicOrdersFilter?.MaxNumberAlgorithmicOrders,
                    MaxNumberOrders = symbol.Symbol.MaxOrdersFilter?.MaxNumberOrders,
                    MaxPosition = symbol.Symbol.MaxPositionFilter?.MaxPosition,
                    MaxPrice = symbol.Symbol.PriceFilter?.MaxPrice,
                    MaxQuantity = symbol.Symbol.MarketLotSizeFilter?.MaxQuantity,
                    MaxTrailingAboveDelta = symbol.Symbol.TrailingDeltaFilter?.MaxTrailingAboveDelta,
                    MaxTrailingBelowDelta = symbol.Symbol.TrailingDeltaFilter?.MaxTrailingBelowDelta,
                    MinNotional = symbol.Symbol.MinNotionalFilter?.MinNotional,
                    MinPrice = symbol.Symbol.PriceFilter?.MinPrice,
                    MinQuantity = symbol.Symbol.MarketLotSizeFilter?.MinQuantity,
                    MinTrailingAboveDelta = symbol.Symbol.TrailingDeltaFilter?.MinTrailingAboveDelta,
                    MinTrailingBelowDelta = symbol.Symbol.TrailingDeltaFilter?.MinTrailingBelowDelta,
                    MultiplierDecimal = symbol.Symbol.PricePercentFilter?.MultiplierDecimal,
                    MultiplierDown = symbol.Symbol.PricePercentFilter?.MultiplierDown,
                    MultiplierUp = symbol.Symbol.PricePercentFilter?.MultiplierUp,
                    OCOAllowed = symbol.Symbol.OCOAllowed,
                    OTOAllowed = symbol.Symbol.OTOAllowed,
                    OrderTypes = symbol.Symbol.OrderTypes,
                    QuoteAsset = symbol.Symbol.QuoteAsset,
                    QuoteAssetPrecision = symbol.Symbol.QuoteAssetPrecision,
                    QuoteFeePrecision = symbol.Symbol.QuoteAssetPrecision,
                    QuoteOrderQuantityMarketAllowed = symbol.Symbol.QuoteOrderQuantityMarketAllowed,
                    Status = symbol.Symbol.Status,
                    StepSize = symbol.Symbol.MarketLotSizeFilter?.StepSize,
                    TickSize = symbol.Symbol.PriceFilter?.TickSize
                }                
            });
    }
}
