
using HftCryptoTrading.Client;
using HftCryptoTrading.Shared;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;

namespace HftCryptoTrading.Saga.MarketDownloader.Workers;

public class MarketDownloaderSaga(AppSettings appSetting)
{
    private readonly string _hubUrl = appSetting.Hub.HubApiUrl;
    private readonly string _hubApiKey = appSetting.Hub.HubApiKey;
    private readonly string _hubApiSecret = appSetting.Hub.HubApiSecret;

    private readonly MessageClientHub<NewSymbolPublishedEvent> _newSymbolPublishedEventHub;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _newSymbolPublishedEventHub.ClientMessageReceived += newSymbolPublishedEventHub_ClientMessageReceived;
        await _newSymbolPublishedEventHub.StartAsync(appSetting.Hub.NameSpace);
    }

    private void newSymbolPublishedEventHub_ClientMessageReceived(object? sender, NewSymbolPublishedEvent e)
    {
        throw new NotImplementedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task PublishSymbols(IEnumerable<SymbolTickerData> data)
    {
        await _newSymbolPublishedEventHub.BroadcastEvent(Guid.NewGuid(), appSetting.Hub.NameSpace, new NewSymbolPublishedEvent(data, DateTime.UtcNow));
    }
}
