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

public class PositionStoreTests
{
    [Fact]
    public void AddPosition_AddsPositionToExchange()
    {
        var _positionStore = new PositionStore();

        // Arrange
        var position = new AccountPosition("Binance", "BTCUSDT")
        {
            Balances = new List<StreamBalance> {
                new StreamBalance() {
                    Asset = "BTCUSDT",
                    Available = 1.5m
                }
            }
        };

        // Act
        _positionStore.AddPosition(position);

        // Assert
        var positions = _positionStore.GetPositions("Binance");
        Assert.Contains(position, positions);
    }

    [Fact]
    public void AddRange_AddsMultiplePositionsToExchange()
    {
        // Arrange
        var exchangeName = "Binance";

        var positions = new List<AccountPosition>
        {
            new(exchangeName, "BTCUSDT")
            {
                Balances =
                [
                    new StreamBalance { Available = 1.5m }
                ]
            },
            new(exchangeName, "ETHUSDT")
            {
                Balances =
                [
                    new StreamBalance { Available = 2.0m }
                ]
            }
        };

        var _positionStore = new PositionStore();

        // Act
        _positionStore.AddRange(exchangeName, positions);

        // Assert
        var storedPositions = _positionStore.GetPositions(exchangeName).ToList();

        Assert.Equal(2, storedPositions.Count);
        Assert.Contains(positions[0], storedPositions);
        Assert.Contains(positions[1], storedPositions);
    }

    [Fact]
    public async Task GetPositionSize_ReturnsPositionSizeForSymbol()
    {
        // Arrange
        var exchangeName = "Binance";
        var symbolName = "BTC";

        var _positionStore = new PositionStore();

        _positionStore.AddPosition(new AccountPosition(exchangeName, symbolName)
        {
            Balances = new List<StreamBalance> { new StreamBalance{ Available = 1.5m } }
        });

        // Act
        var positionSize = await _positionStore.GetPositionSize(exchangeName, symbolName, CancellationToken.None);

        // Assert
        Assert.Equal(1.5m, positionSize);
    }

    [Fact]
    public async Task GetPositionSize_ReturnsZeroIfPositionNotFound()
    {
        // Arrange
        var exchangeName = "Binance";
        var symbolName = "BTC";
        var _positionStore = new PositionStore();

        // Act
        var positionSize = await _positionStore.GetPositionSize(exchangeName, symbolName, CancellationToken.None);

        // Assert
        Assert.Equal(0.0m, positionSize);
    }

    [Fact]
    public async Task SetPosition_UpdatesExistingPosition()
    {
        // Arrange
        var exchangeName = "Binance";
        var symbolName = "BTC";
        
        var initialPosition = new AccountPosition(exchangeName, symbolName)
        {
            Balances = new List<StreamBalance> { new StreamBalance { Available = 1.5m } }
        };

        var _positionStore = new PositionStore();

        _positionStore.AddPosition(initialPosition);

        var updateEvent = new AccountPositionUpdateEvent(exchangeName, symbolName,
            new AccountPosition(exchangeName, symbolName)
            {
                Timestamp = DateTime.UtcNow,
                ListenKey = "updatedKey",
                Balances = new List<StreamBalance> { new StreamBalance { Available = 2.5m } }
            });

        // Act
        await _positionStore.SetPosition(updateEvent);

        // Assert
        var updatedPosition = _positionStore.GetPositions(exchangeName).FirstOrDefault(p => p.Symbol == symbolName);

        Assert.NotNull(updatedPosition);
        Assert.Equal(updateEvent.Position.ListenKey, updatedPosition.ListenKey);
        Assert.Equal(updateEvent.Position.Balances.First().Available, updatedPosition.Balances.First().Available);
        Assert.Equal(updateEvent.Position.Timestamp, updatedPosition.Timestamp);
    }
}
