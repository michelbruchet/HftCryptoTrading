using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Events;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.MarketWatcher.Handlers;

public class PublishSymbolsDownloadedAnalysedSuccessFullyHandler(ExchangeProviderFactory exchangeProvider, 
    IOptions<AppSettings> appSettings, 
    ILoggerFactory loggerFactory) : IRequestHandler<PublishSymbolAnalysedSuccessFullyEvent>
{
    ExchangeProviderFactory _exchangeProvider = exchangeProvider;

    public async Task Handle(PublishSymbolAnalysedSuccessFullyEvent notification, CancellationToken cancellationToken)
    {
        var exchange = _exchangeProvider.GetExchange(notification.ExchangeName, appSettings.Value, loggerFactory) 
            ?? throw new PlatformNotSupportedException(nameof(notification.ExchangeName));

        await exchange.RegisterPriceChangeHandlerAsync(notification);        
    }
}
