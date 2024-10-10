using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using System.Collections.Concurrent;

namespace hftCryptoTrading.Saga.OpenPositionMonitor.Services;

public interface IPositionStore
{
    void AddPosition(AccountPosition openPosition);
    void AddRange(string exchangeName, List<AccountPosition> positions);
    Task<decimal> GetPositionSize(string exchangeName, string symbolName, CancellationToken cancellationToken);
    Task SetPosition(AccountPositionUpdateEvent notification);
}

public class PositionStore : IPositionStore
{
    private readonly ConcurrentDictionary<string, List<AccountPosition>> _positions = new();

    public void AddPosition(AccountPosition openPosition)
    {
        ArgumentNullException.ThrowIfNull(openPosition);

        if (_positions.ContainsKey(openPosition.Exchange))
            _positions[openPosition.Exchange].Add(openPosition);
        else
            _positions.TryAdd(openPosition.Exchange, new List<AccountPosition>(new [] {openPosition}));
    }

    public void AddRange(string exchangeName, List<AccountPosition> positions)
    {
        ArgumentNullException.ThrowIfNull(exchangeName);
        ArgumentNullException.ThrowIfNull(positions);

        if(_positions.ContainsKey(exchangeName))
            _positions[exchangeName].AddRange(positions);
        else
            _positions.TryAdd(exchangeName, positions);
    }

    public IEnumerable<AccountPosition> GetPositions(string exchangeName)
    {
        return _positions.TryGetValue(exchangeName, out var positions) ? positions : Enumerable.Empty<AccountPosition>();
    }

    public async Task<decimal> GetPositionSize(string exchangeName, string symbolName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exchangeName);
        ArgumentNullException.ThrowIfNull(symbolName);

        if (!_positions.ContainsKey(exchangeName))
            return 0;

        if (!_positions[exchangeName].Any()) return 0;

        if (!_positions[exchangeName].Any(p=>p.Symbol == symbolName)) return 0;

        return _positions[exchangeName].FirstOrDefault(p=>p.Symbol == symbolName).Balances.FirstOrDefault().Available;
    }

    public async Task SetPosition(AccountPositionUpdateEvent notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        await Task.Run(() =>
        {
            if (!_positions.ContainsKey(notification.Exchange))
                _positions.TryAdd(notification.Exchange, []);

            if (_positions[notification.Exchange].Count == 0)
            {
                _positions[notification.Exchange] = [];
            }

            if (!_positions[notification.Exchange].Any(p => p.Symbol == notification.Symbol))
                _positions[notification.Exchange].Add(new AccountPosition(notification.Exchange, notification.Symbol));

            var position = _positions[notification.Exchange]
                .First(p => p.Symbol == notification.Symbol);

            position.Timestamp = notification.Position.Timestamp;
            position.ListenKey = notification.Position.ListenKey;
            position.Balances = notification.Position.Balances;
        });
    }
}