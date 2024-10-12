using HftCryptoTrading.Customers.Shared;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Services.Commands;
using MediatR;
using Microsoft.Extensions.Options;

namespace HftCryptoTrading.Saga.OpenPositionMonitor.Handlers;

public class DownloadOpenOrdersRequestHandler(IServiceProvider serviceProvider) : IRequestHandler<DownloadOpenOrdersRequest>
{
    IServiceProvider _serviceProvider = serviceProvider;

    public async Task Handle(DownloadOpenOrdersRequest request, CancellationToken cancellationToken)
    {
        _serviceProvider = _serviceProvider.CreateScope().ServiceProvider;

        var downloadOpenOrderCommand = new DownloadOpenOrderCommand(request.Container.ExchangeClient, request.Container.AppSettings);
        
        await downloadOpenOrderCommand.ExecuteAsync(request, cancellationToken);
    }
}