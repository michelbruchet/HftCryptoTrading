using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.Services;

using hftCryptoTrading.Saga.OpenPositionMonitor.Services;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class OrderStoreTests
{
 
    [Fact]
    public void AddOrder_AddsOrderToExchange()
    {
        var _orderStore = new OrderStore();

        // Arrange
        var order = new OpenOrder("BTCUSDT", "Binance")
        {
            ClientOrderId = "123",
            QuantityFilled = 1.5m
        };

        // Act
        _orderStore.AddOrder(order);

        // Assert
        var openOrders = _orderStore.GetOpenOrders("Binance");
        Assert.Contains(order, openOrders);
    }

    [Fact]
    public void AddRange_AddsMultipleOrders()
    {
        // Arrange
        var _orderStore = new OrderStore();

        var exchangeName = "Binance";
        var orders = new List<OpenOrder>
        {
            new OpenOrder("BTCUSDT", exchangeName){ ClientOrderId = "123", QuantityFilled = 1.5m },
            new OpenOrder ("ETHUSDT", exchangeName) { ClientOrderId = "456", QuantityFilled = 2.0m }
        };

        // Act
        _orderStore.AddRange(exchangeName, orders);

        // Assert
        var openOrders = _orderStore.GetOpenOrders(exchangeName);

        Assert.Equal(2, openOrders.Count);
        Assert.Contains(orders[0], openOrders);
        Assert.Contains(orders[1], openOrders);
    }

    [Fact]
    public async Task GetOpenOrdersSize_ReturnsTotalSizeForSymbol()
    {
        // Arrange
        var _orderStore = new OrderStore();

        var exchangeName = "Binance";
        var symbolName = "BTCUSDT";

        _orderStore.AddOrder(new OpenOrder(symbolName, exchangeName){QuantityFilled = 1.5m });
        _orderStore.AddOrder(new OpenOrder (symbolName, exchangeName) { QuantityFilled = 2.5m });

        // Act
        var totalSize = await _orderStore.GetOpenOrdersSize(exchangeName, symbolName, CancellationToken.None);

        // Assert
        Assert.Equal(4.0m, totalSize);
    }

    [Fact]
    public async Task GetOpenOrdersSize_ReturnsZeroWhenSymbolNotFound()
    {
        // Arrange
        var _orderStore = new OrderStore();

        var exchangeName = "Binance";
        var symbolName = "BTC";

        // Act
        var totalSize = await _orderStore.GetOpenOrdersSize(exchangeName, symbolName, CancellationToken.None);

        // Assert
        Assert.Equal(0.0m, totalSize);
    }

    [Fact]
    public async Task UpdateOrder_UpdatesExistingOrder()
    {
        // Arrange
        var _orderStore = new OrderStore();

        var exchangeName = "Binance";
        var order = new OpenOrder("BTCUSDT", exchangeName)
        {
            ClientOrderId = "123",
            QuantityFilled = 1.5m,
            IsWorking = true
        };

        _orderStore.AddOrder(order);

        var updateNotification = new OrderUpdateEvent(exchangeName, "BTCUSDT", new(exchangeName, "BTCUSDT")
        {
            ClientOrderId = "123",
            IsWorking = false,
            QuantityFilled = 2.0m,
            QuoteQuantityFilled = 4000.0m,
            Status = OrderStatus.Filled
        });

        // Act
        await _orderStore.UpdateOrder(updateNotification);

        // Assert
        var updatedOrder = _orderStore.GetOpenOrders(exchangeName).FirstOrDefault(o => o.ClientOrderId == "123");

        Assert.NotNull(updatedOrder);
        Assert.False(updatedOrder.IsWorking);
        Assert.Equal(2.0m, updatedOrder.QuantityFilled);
        Assert.Equal(4000.0m, updatedOrder.QuoteQuantityFilled);
        Assert.Equal(OrderStatus.Filled, updatedOrder.Status);
    }
}
