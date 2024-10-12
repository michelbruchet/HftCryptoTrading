using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Services.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HftCryptoTrading.Saga.OpenPositionMonitor.Handlers;

public class DownloadBalanceRequestHandler(IServiceProvider serviceProvider) : IRequestHandler<DownloadBalanceRequest>
{
    IServiceProvider _serviceProvider = serviceProvider;

    public async Task Handle(DownloadBalanceRequest request, CancellationToken cancellationToken)
    {
        _serviceProvider = _serviceProvider.CreateScope().ServiceProvider;

        var downloadBalanceCommand = new DownloadBalanceCommand(request.Container.ExchangeClient, request.Container.AppSettings);
        await downloadBalanceCommand.ExecuteAsync(request, cancellationToken);
    }
}