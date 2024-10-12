using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using MediatR;
using Microsoft.Extensions.Options;
using HftCryptoTrading.Client;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Saga;
using HftCryptoTrading.Services.Processes;

public class MarketWatcherSaga : IMarketWatcherSaga
{
    private readonly AppSettings _appSetting;
    private readonly IMediator _mediator;
    private readonly IExchangeProviderFactory _factory;
    private readonly IMetricService _metricService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<Task> _downloadTasks = new(); // List to store ongoing tasks
    private readonly CancellationTokenSource _cancellationTokenSource = new(); // Cancellation source to stop tasks
    private readonly TimeSpan _delayBetweenDownloads = TimeSpan.FromHours(5); // Interval of 5 hours between downloads
    private readonly IHubClientPublisher _publisher;

    public MarketWatcherSaga(IOptions<AppSettings> appSetting, 
        IMediator mediator, ILoggerFactory loggerFactory, 
        IExchangeProviderFactory factory, IMetricService metricService, IHubClientPublisherFactory hubClientPublisherFactory)
    {
        _appSetting = appSetting.Value;
        _mediator = mediator;
        _factory = factory;
        _metricService = metricService;
        _loggerFactory = loggerFactory;

        _publisher = hubClientPublisherFactory.Initialize(_appSetting, typeof(SymbolAnalysedSuccessFullyEvent).Name);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //Connect to the hub
        await _publisher.StartAsync(_appSetting.Hub.NameSpace);

        // Combine internal and external cancellation tokens
        using var linkedCancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

        // Retrieve all registered exchanges
        var exchangeNames = _factory.GetAllExchanges();

        Parallel.ForEach(exchangeNames, async (exchangeName) =>
        {
            var exchangeClient = _factory.GetExchange(exchangeName, _appSetting, _loggerFactory)!;
            var downloadSymbolCommand = new DownloadSymbolCommand(exchangeClient, _metricService, this, _appSetting);
            await downloadSymbolCommand.ExecuteAsync(cancellationToken);
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        await Task.WhenAll(_downloadTasks);
    }

    public async Task PublishDownloadedSymbols(string exchangeName, IEnumerable<SymbolTickerData> data)
    {
        await _mediator.Send(new PublishedDownloadedSymbolsEvent(exchangeName, data));
    }

    public async Task PublishDownloadedSymbolAnalysedSuccessFully(string exchange, List<SymbolTickerData> validSymbols)
    {
        await _mediator.Send(new PublishSymbolAnalysedSuccessFullyEvent(exchange, validSymbols));
    }

    public Task PublishDownloadedSymbolAnalysedPriceFailed(string exchangeName, List<SymbolTickerData> abnormalPriceSymbols)
    {
        //TODO V2 : Manage abnormal price symbols
        return Task.CompletedTask;
    }

    public Task PublishDownloadedSymbolAnalysedSpreadBidAskFailed(string exchangeName, List<SymbolTickerData> abnormalSpreadSymbols)
    {
        //TODO V2 : Manage abnormal spread symbols
        return Task.CompletedTask;
    }

    public Task PublishDownloadedSymbolAnalysedVolumeFailed(string exchangeName, List<SymbolTickerData> abnormalVolumeSymbols)
    {
        //TODO V2 : Manage abnormal volume symbols
        return Task.CompletedTask;
    }


    public Task PublishStreamedSymbolAnalysedPriceFailed(string exchangeName, SymbolTickerData abnormalPriceSymbols)
    {
        //TODO V2 : Manage abnormal Price
        return Task.CompletedTask;
    }

    public Task PublishStreamedSymbolAnalysedSpreadBidAskFailed(string exchangeName, SymbolTickerData abnormalSpreadSymbol)
    {
        //TODO V2 : Manage abnormal Spread bid ask
        return Task.CompletedTask;
    }

    public Task PublishStreamedSymbolAnalysedVolumeFailed(string exchangeName, SymbolTickerData abnormalVolumeSymbol)
    {
        //TODO V2 : Manage abnormal volume symbols
        return Task.CompletedTask;
    }

    public async Task PublishStreamedSymbolAnalysedSuccessFully(string exchangeName, SymbolTickerData validSymbol)
    {
        await _publisher.BroadcastEvent<SymbolAnalysedSuccessFullyEvent>(Guid.NewGuid(), _appSetting.Hub.NameSpace,
            new SymbolAnalysedSuccessFullyEvent(exchangeName, validSymbol.Symbol)
            {
                BestAskPrice = validSymbol.BookPrice.BestAskPrice,
                BestAskQuantity = validSymbol.BookPrice.BestAskQuantity,
                BestBidPrice = validSymbol.BookPrice.BestBidPrice,
                BestBidQuantity = validSymbol.BookPrice.BestBidQuantity,
                HighPrice = validSymbol.Ticker.HighPrice,
                LowPrice = validSymbol.Ticker.LowPrice,
                Price = validSymbol.Ticker.LastPrice,
                PriceChangePercent = validSymbol.PriceChangePercent,
                PublishedDate = validSymbol.PublishedDate,
                Volume = validSymbol.Ticker.Volume
            });
    }
}
