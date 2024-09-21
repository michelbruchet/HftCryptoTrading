using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.MarketDownloader.Processes;
using HftCryptoTrading.Saga.MarketDownloader.Workers;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared;
using MediatR;
using Microsoft.Extensions.Options;

public class MarketDownloaderSaga : IMarketDownloaderSaga
{
    private readonly AppSettings _appSetting;
    private readonly IMediator _mediator;
    private readonly ExchangeProviderFactory _factory;
    private readonly IMetricService _metricService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<Task> _downloadTasks = new(); // List to store ongoing tasks
    private readonly CancellationTokenSource _cancellationTokenSource = new(); // Cancellation source to stop tasks
    private readonly TimeSpan _delayBetweenDownloads = TimeSpan.FromHours(5); // Interval of 5 hours between downloads

    public MarketDownloaderSaga(IOptions<AppSettings> appSetting, IMediator mediator, ILoggerFactory loggerFactory, ExchangeProviderFactory factory, IMetricService metricService)
    {
        _appSetting = appSetting.Value;
        _mediator = mediator;
        _factory = factory;
        _metricService = metricService;
        _loggerFactory = loggerFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
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

    public async Task PublishSymbols(IEnumerable<SymbolTickerData> data)
    {
        await _mediator.Publish(new NewSymbolTickerDataEvent(data));
    }
}
