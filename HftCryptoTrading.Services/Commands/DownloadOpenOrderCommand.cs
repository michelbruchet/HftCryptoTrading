using HftCryptoTrading.Customers.Shared;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Shared.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Services.Commands;

public interface IOpenPositionMonitorSagaContainer
{
    AppSettings AppSettings { get; }
    IExchangeClient ExchangeClient { get; }

    Task StartAsync(CancellationToken cancellationToken);
    void SetOpenOrders(List<OpenOrder> openOrders);
    void SetPositions(AccountPosition positions);
    void SetBalances(List<AccountBalance> balances);
}

public class DownloadOpenOrdersRequest(Customer customer, IOpenPositionMonitorSagaContainer container) : IRequest
{
    public Customer Customer { get; } = customer;
    public IOpenPositionMonitorSagaContainer Container { get; } = container;
}

public class DownloadPositionsRequest(Customer customer, IOpenPositionMonitorSagaContainer container) : IRequest
{
    public Customer Customer { get; } = customer;
    public IOpenPositionMonitorSagaContainer Container { get; } = container;
}

public class DownloadBalanceRequest(Customer customer, IOpenPositionMonitorSagaContainer container) : IRequest
{
    public Customer Customer { get; } = customer;
    public IOpenPositionMonitorSagaContainer Container { get; } = container;
}

public class DownloadOpenOrderCommand(IExchangeClient exchangeClient, AppSettings appSettings)
{
    public async Task ExecuteAsync(DownloadOpenOrdersRequest request, 
        CancellationToken cancellationToken)
    {
        var openOrders = await exchangeClient.GetOpenedOrders();
        request.Container.SetOpenOrders(openOrders);
    }
}


public class DownloadPositionCommand(IExchangeClient exchangeClient, AppSettings appSettings)
{
    public async Task ExecuteAsync(DownloadPositionsRequest request,
        CancellationToken cancellationToken)
    {
        var positions = await exchangeClient.GetCurrentPositions();
        request.Container.SetPositions(positions);
    }
}

public class DownloadBalanceCommand(IExchangeClient exchangeClient, AppSettings appSettings)
{
    public async Task ExecuteAsync(DownloadBalanceRequest request,
        CancellationToken cancellationToken)
    {
        var balances = await exchangeClient.GetCurrentAccountBalancesGroupedByBaseAsset();
        request.Container.SetBalances(balances);
    }
}