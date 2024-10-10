
using HftCryptoTrading.Client;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using MediatR;
using Microsoft.Extensions.Options;
using StrategyExecution;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Workers;

public class StrategyAnalyserSaga : IStrategyAnalyserSaga
{
    private readonly string _indicatorsPath;
    private readonly string _strategyPath;
    private readonly AppSettings _appSetting;
    private readonly IMediator _mediator;
    private readonly ExchangeProviderFactory _factory;
    private readonly IMetricService _metricService;
    private readonly ILogger<StrategyAnalyserSaga> _logger; // Logger pour la journalisation
    private readonly HubClientPublisher _publisher;
    private readonly HubClientReceiver<SymbolAnalysedSuccessFullyEvent> _receiver;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public StrategyAnalyserSaga(IOptions<AppSettings> appSettings, IMediator mediator,
        ILogger<StrategyAnalyserSaga> logger, ExchangeProviderFactory factory, IMetricService metricService)
    {
        _appSetting = appSettings.Value;
        _mediator = mediator;
        _factory = factory;
        _metricService = metricService;
        _logger = logger;

        _publisher = new HubClientPublisher(_appSetting);
        _receiver = new HubClientReceiver<SymbolAnalysedSuccessFullyEvent>(_appSetting);
        _receiver.ClientMessageReceived += SymbolAnalyseReceived;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        IndicatorLoaderService.Service.LoadIndicators(_appSetting.Runtime.IndicatorsPath);

        // Connect to the hub
        using var scope = _metricService.StartTracking("StartAsync");
        try
        {
            await _publisher.StartAsync(_appSetting.Hub.NameSpace);
            await _receiver.StartAsync();

            _metricService.TrackSuccess("StartAsync");
        }
        catch (Exception ex)
        {
            _metricService.TrackFailure("StartAsync", ex);
            _logger.LogError(ex, "Error starting StrategyAnalyserSaga");
        }
    }

    private void SymbolAnalyseReceived(object? sender, SymbolAnalysedSuccessFullyEvent e)
    {
        _logger.LogInformation($"Received SymbolAnalysedSuccessFullyEvent for symbol {e.SymbolData.Name}");
        _mediator.Publish(e);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping StrategyAnalyserSaga");
        return Task.CompletedTask; // Optionnel : Ajouter des logiques d'arrêt si nécessaire
    }

    public async Task PublishHistoricalKlines(SymbolAnalysedSuccessFullyEvent @event, List<KlineData> history)
    {
        using var scope = _metricService
            .StartTracking("PublishHistoricalKlines");

        try
        {
            await _mediator
                .Publish(new DownloadSymbolHistoryEvent(@event, history));
            
            _metricService.TrackSuccess("PublishHistoricalKlines");
        }
        catch (Exception ex)
        {
            _metricService.TrackFailure("PublishHistoricalKlines", ex);
            _logger.LogError(ex, "Error publishing historical klines");
        }
    }

    public async Task PublishStrategyResult(ActionStrategy result, DownloadSymbolHistoryEvent notification)
    {
        using var scope = _metricService.StartTracking("PublishStrategyResult");
        try
        {
            switch (result)
            {
                case ActionStrategy.Long:
                    await _publisher
                        .BroadcastEvent<LongTradeSymbolDetectedEvent>(Guid.NewGuid(), _appSetting.Hub.NameSpace, notification);
                    break;
                case ActionStrategy.Short:
                    await _publisher
                        .BroadcastEvent<ShortTradeSymbolDetectedEvent>(Guid.NewGuid(), _appSetting.Hub.NameSpace, notification);
                    break;
                default:
                    break;
            }

            _metricService.TrackSuccess("PublishStrategyResult");
        }
        catch (Exception ex)
        {
            _metricService.TrackFailure("PublishStrategyResult", ex);
            _logger.LogError(ex, "Error publishing strategy result");
        }
    }
}
