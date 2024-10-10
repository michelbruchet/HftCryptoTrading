using HftCryptoTrading.Services.Commands;
using HftCryptoTrading.Services.Processes;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.MarketWatcher.Handlers;

public class PriceChangeDetectedHandler(
    IServiceProvider serviceProvider,
    IOptions<AppSettings> appSettings,
    ILogger<PublishedSymbolsDownloadedHandler> logger)
    : INotificationHandler<PriceChangeDetectedEvent>
{
    public async Task Handle(PriceChangeDetectedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var analyseSymbolDownloadedCommand =
                ActivatorUtilities.
                    GetServiceOrCreateInstance<AnalysePriceChangeDetectedCommand>
                        (serviceProvider) ?? throw
                            new PlatformNotSupportedException("can not instanciate analyse command");

            await analyseSymbolDownloadedCommand
                .RunAsync(new SymbolTickerData(notification.ExchangeName, notification.Symbol)
                {
                    BookPrice = new BookPriceData(notification.Symbol.Name)
                    {
                        BestAskPrice = notification.BestAskPrice,
                        BestAskQuantity = notification.BestAskQuantity,
                        BestBidPrice = notification.BestBidPrice,
                        BestBidQuantity = notification.BestBidQuantity,
                    },
                    PriceChangePercent = notification.PriceChangePercent,
                    PublishedDate = notification.CloseTime,
                    Ticker = new TickerData(notification.Symbol.Name, notification.ExchangeName)
                    {
                        Ask = notification.BestAskPrice,
                        Bid = notification.BestBidPrice,
                        ChangePercentage = notification.PriceChangePercent,
                        HighPrice = notification.HighPrice,
                        LastPrice = notification.LastPrice,
                        LowPrice = notification.LowPrice,
                        Volume = notification.Volume,
                    }
                });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
        }
    }
}
