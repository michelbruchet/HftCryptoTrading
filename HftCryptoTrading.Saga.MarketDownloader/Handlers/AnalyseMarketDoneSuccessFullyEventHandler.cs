using HftCryptoTrading.Exchanges.Core.Events;
using HftCryptoTrading.Exchanges.Core.Exchange;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.MarketDownloader.Handlers;

public class AnalyseMarketDoneSuccessFullyEventHandler(ExchangeProviderFactory exchangeProvider, 
    IOptions<AppSettings> appSettings, 
    ILoggerFactory loggerFactory) : INotificationHandler<AnalyseMarketDoneSuccessFullyEvent>
{
    ExchangeProviderFactory _exchangeProvider = exchangeProvider;

    public async Task Handle(AnalyseMarketDoneSuccessFullyEvent notification, CancellationToken cancellationToken)
    {
        var exchange = _exchangeProvider.GetExchange(notification.ExchangeName, appSettings.Value, loggerFactory) 
            ?? throw new PlatformNotSupportedException(nameof(notification.ExchangeName));

        await exchange.RegisterPriceChangeHandlerAsync(notification);        
    }
}
