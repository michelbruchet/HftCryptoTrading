using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Requests;
using HftCryptoTrading.Customers.Shared;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.OpenPositionMonitor.Handlers;
using HftCryptoTrading.Services.Commands;
using HftCryptoTrading.Shared.Models;
using MediatR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace HftCryptoTrading.Saga.OpenPositionMonitor;

public class OpenPositionMonitorSagaContainer : IOpenPositionMonitorSagaContainer
{
    IServiceProvider _serviceProvider;
    Customer _customer;
    ConcurrentDictionary<string, OpenOrder> _openOrders = new();
    ConcurrentDictionary<string, StreamBalance> _accountPositions = new();
    ConcurrentDictionary<string, AccountBalance> _accountBalances = new();

    public OpenPositionMonitorSagaContainer(IServiceProvider serviceProvider, Customer customer)
    {
        _serviceProvider = serviceProvider.CreateScope().ServiceProvider;
        _customer = customer;
    }

    public AppSettings AppSettings { get; private set; }
    public IExchangeClient ExchangeClient { get; private set; }

    public void SetBalances(List<AccountBalance> balances)
    {
        _accountBalances = new ConcurrentDictionary<string, AccountBalance>(balances.Select(b => new
            KeyValuePair<string, AccountBalance>(b.Symbol, b)));
    }

    public void SetOpenOrders(List<OpenOrder> openOrders)
    {
        _openOrders = new ConcurrentDictionary<string, OpenOrder>(openOrders.Select(o =>
            new KeyValuePair<string, OpenOrder>(o.ClientOrderId, o)));
    }

    public void SetPositions(AccountPosition positions)
    {
        foreach (var position in positions.Balances)
        {
            _accountPositions.TryAdd(position.Asset, position);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var exchangeFactory = _serviceProvider.GetRequiredService<IExchangeProviderFactory>();
        var platformSettings = _serviceProvider.GetRequiredService<IOptions<AppSettings>>();

        AppSettings = new AppSettings
        {
            Exchange = new ExchangeSettings
            {
                ApiKey = _customer.ApiKey,
                ApiSecret = _customer.ApiSecret,
                IsBackTest = _customer.IsBackTest
            }
        };

        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

        ExchangeClient = exchangeFactory
            .GetExchange(_customer.ExchangeName, AppSettings, loggerFactory)!;

        ExchangeClient.OnOrderUpdated += ExchangeClient_OnOrderUpdated;
        ExchangeClient.OnAccountPositionUpdated += ExchangeClient_OnAccountPositionUpdated;

        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new DownloadOpenOrdersRequest(_customer, this));
        await mediator.Send(new DownloadPositionsRequest(_customer, this));
        await mediator.Send(new DownloadBalanceRequest(_customer, this));

        await ExchangeClient.TrackUserStream();
    }

    private void ExchangeClient_OnAccountPositionUpdated(object? sender, Shared.Events.AccountPositionUpdateEvent e)
    {
        foreach (var position in e.Position.Balances)
        {
            if (!_accountPositions.ContainsKey(position.Asset))
                _accountPositions.TryAdd(position.Asset, position);
            else
                _accountPositions[position.Asset] = position;
        }
    }

    private void ExchangeClient_OnOrderUpdated(object? sender, Shared.Events.OrderUpdateEvent e)
    {
        if (_openOrders.ContainsKey(e.Position.ClientOrderId))
        {
            _openOrders[e.Position.ClientOrderId].Update(e.Position);
        }
        else
        {
            _openOrders.TryAdd(e.Position.ClientOrderId, new OpenOrder(e.Position.Symbol, e.Position.Exchange)
            {
                ClientOrderId = e.Position.ClientOrderId,
                CreateTime = e.Position.CreateTime,
                IcebergQuantity = e.Position.IcebergQuantity,
                IsWorking = e.Position.IsWorking,
                OrderListId = e.Position.OrderListId,
                OriginalClientOrderId = e.Position.OriginalClientOrderId,
                Price = e.Position.Price,
                Quantity = e.Position.Quantity,
                QuantityFilled = e.Position.QuantityFilled,
                QuoteQuantity = e.Position.QuoteQuantity,
                QuoteQuantityFilled = e.Position.QuoteQuantityFilled,
                SelfTradePreventionMode = (int)e.Position.SelfTradePreventionMode,
                Side = (int)e.Position.Side,
                Status = e.Position.Status,
                StopPrice = e.Position.StopPrice,
                Id = e.Position.Id,
                TimeInForce = (int)e.Position.TimeInForce,
                Type = (int)e.Position.Type,
                UpdateTime = e.Position.UpdateTime,
                WorkingTime = e.Position.WorkingTime
            });
        }
    }
}
