using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using System.Collections.Concurrent;
using System.Reactive;

namespace hftCryptoTrading.Saga.OpenPositionMonitor.Services;

public interface IAccountBalanceStore
{
    void AddRange(string exchangeName, IEnumerable<AccountBalance> balances);
    Task SetBalance(AccountBalanceUpdateEvent notification, CancellationToken cancellationToken);
}

public class AccountBalanceStore : IAccountBalanceStore
{
    private readonly ConcurrentDictionary<string, AccountBalance> _balances = new();

    public void AddRange(string exchangeName, IEnumerable<AccountBalance> balances)
    {
        if (balances == null) return;

        Parallel.ForEach(balances, balance =>
        {
            var key = $"{exchangeName}_[{balance.Symbol}]";
            _balances.AddOrUpdate(
                key,
                new AccountBalance(exchangeName, balance.Symbol)
                {
                    ListenKey = balance.ListenKey,
                    Available = balance.Available
                },
                (k, existingBalance) =>
                {
                    existingBalance.ListenKey = balance.ListenKey;
                    existingBalance.Available += balance.Available;
                    return existingBalance;
                });
        });
    }

    public bool ContainsBalance(string exchangeName, string symbol)
    {
        var key = $"{exchangeName}_[{symbol}]";
        return _balances.TryGetValue(key, out var _);
    }

    public IEnumerable<AccountBalance> GetAllBalances()
        => _balances.Values;

    public AccountBalance GetBalance(string exchangeName, string symbol)
        => _balances.TryGetValue($"{exchangeName}_[{symbol}]", out var balance)
        ? balance : null; 

    public Task SetBalance(AccountBalanceUpdateEvent notification, CancellationToken cancellationToken)
    {
        var key = $"{notification.Exchange}_[{notification.Symbol}]";

        _balances.AddOrUpdate(
            key,
            new AccountBalance(notification.Exchange, notification.Symbol)
            {
                ListenKey = notification.AccountBalance.ListenKey,
                Available = notification.AccountBalance.BalanceDelta
            },
            (k, existingBalance) =>
            {
                existingBalance.ListenKey = notification.AccountBalance.ListenKey;
                existingBalance.Available += notification.AccountBalance.BalanceDelta;
                return existingBalance;
            });

        return Task.CompletedTask;
    }
}
