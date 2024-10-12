using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Services.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HftCryptoTrading.Saga.OpenPositionMonitor.Handlers;

public class DownloadPositionRequestHandler(IServiceProvider serviceProvider) : IRequestHandler<DownloadPositionsRequest>
{
    IServiceProvider _serviceProvider = serviceProvider;

    public async Task Handle(DownloadPositionsRequest request, CancellationToken cancellationToken)
    {
        _serviceProvider = _serviceProvider.CreateScope().ServiceProvider;

        var downloadPositionRequestCommand = new DownloadPositionCommand(request.Container.ExchangeClient, request.Container.AppSettings);
        await downloadPositionRequestCommand.ExecuteAsync(request, cancellationToken);
    }
}