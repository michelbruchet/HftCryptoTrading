using hftCryptoTrading.Saga.OpenPositionMonitor.Services;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.Services;

public class AccountBalanceStoreTests
{
    private readonly AccountBalanceStore _accountBalanceStore;

    public AccountBalanceStoreTests()
    {
        _accountBalanceStore = new AccountBalanceStore();
    }

    [Fact]
    public void AddRange_AddsNewBalances()
    {
        // Arrange
        var exchangeName = "Exchange";
        var balances = new List<AccountBalance>
        {
            new AccountBalance(exchangeName, "BTC") { ListenKey = "key1", Available = 1.5m },
            new AccountBalance(exchangeName, "ETH") { ListenKey = "key2", Available = 3.0m }
        };

        // Act
        _accountBalanceStore.AddRange(exchangeName, balances);

        // Assert
        Assert.True(_accountBalanceStore.ContainsBalance(exchangeName, "BTC"));
        Assert.True(_accountBalanceStore.ContainsBalance(exchangeName, "ETH"));
    }

    [Fact]
    public void AddRange_UpdatesExistingBalances()
    {
        // Arrange
        var exchangeName = "Exchange";
        var initialBalances = new List<AccountBalance>
        {
            new AccountBalance(exchangeName, "BTC") { ListenKey = "key1", Available = 1.5m }
        };
        _accountBalanceStore.AddRange(exchangeName, initialBalances);

        var updatedBalances = new List<AccountBalance>
        {
            new AccountBalance(exchangeName, "BTC") { ListenKey = "key1", Available = 2.0m }
        };

        // Act
        _accountBalanceStore.AddRange(exchangeName, updatedBalances);

        // Assert
        var resultBalance = _accountBalanceStore.GetBalance(exchangeName, "BTC");
        Assert.Equal(3.5m, resultBalance.Available);
    }

    [Fact]
    public async Task SetBalance_UpdatesExistingBalance()
    {
        // Arrange
        var exchangeName = "Exchange";
        var initialBalance = new AccountBalance(exchangeName, "BTC") { ListenKey = "key1", Available = 1.5m };
        _accountBalanceStore.AddRange(exchangeName, new[] { initialBalance });

        var notification = new AccountBalanceUpdateEvent(exchangeName, 
                "BTC", new AccountBalanceUpdate(exchangeName, "BTC") 
                { ListenKey = "key1", BalanceDelta = 0.5m });

        // Act
        await _accountBalanceStore.SetBalance(notification, CancellationToken.None);

        // Assert
        var resultBalance = _accountBalanceStore.GetBalance(exchangeName, "BTC");
        Assert.Equal(2.0m, resultBalance.Available);
    }

    [Fact]
    public async Task SetBalance_AddsNewBalanceIfNotExists()
    {
        // Arrange
        var exchangeName = "Exchange";
        var notification = new AccountBalanceUpdateEvent(exchangeName, "ETH", 
            new AccountBalanceUpdate(exchangeName, "ETH") { ListenKey = "key2", BalanceDelta = 1.0m });

        // Act
        await _accountBalanceStore.SetBalance(notification, CancellationToken.None);

        // Assert
        var resultBalance = _accountBalanceStore.GetBalance(exchangeName, "ETH");
        Assert.Equal(1.0m, resultBalance.Available);
    }

    [Fact]
    public void AddRange_DoesNothingWhenBalancesAreNull()
    {
        // Arrange
        var exchangeName = "Exchange";
        IEnumerable<AccountBalance> balances = null;

        // Act
        _accountBalanceStore.AddRange(exchangeName, balances);

        // Assert
        Assert.Empty(_accountBalanceStore.GetAllBalances());
    }
}