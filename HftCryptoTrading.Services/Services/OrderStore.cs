using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Models;
using System.Collections.Concurrent;

namespace hftCryptoTrading.Saga.OpenPositionMonitor.Services;

public interface IOrderStore
{
    void AddOrder(OpenOrder openPosition);
    void AddRange(string exchangeName, List<OpenOrder> positions);
    Task<decimal> GetOpenOrdersSize(string exchangeName, string symbolName, CancellationToken cancellationToken);
    Task UpdateOrder(OrderUpdateEvent notification);
}

public class OrderStore : IOrderStore
{
    private ConcurrentDictionary<string, List<OpenOrder>> _openOrders = new();

    public void AddOrder(OpenOrder openPosition)
    {
        if(_openOrders.ContainsKey(openPosition.Exchange))
            _openOrders[openPosition.Exchange].Add(openPosition);
        else
            _openOrders.TryAdd(openPosition.Exchange, new List<OpenOrder>{openPosition});
    }

    public void AddRange(string exchangeName, List<OpenOrder> positions)
    {
        _openOrders.TryAdd(exchangeName, new List<OpenOrder>(positions));
    }

    public List<OpenOrder> GetOpenOrders(string exchange)
    {
        return _openOrders.TryGetValue(exchange, out var openOrders) ? openOrders: null;
    }

    public async Task<decimal> GetOpenOrdersSize(string exchangeName, string symbolName, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            if (_openOrders.TryGetValue(exchangeName, out List<OpenOrder> positions))
            {
                if (positions.Any(po => po.Symbol == symbolName))
                    return positions.Where(po => po.Symbol == symbolName).Sum(p => p.QuantityFilled);
            }

            return 0.0m;
        });
    }

    public async Task UpdateOrder(OrderUpdateEvent notification)
    {
        if(_openOrders.TryGetValue(notification.Exchange, out List<OpenOrder> positions))
        {
            if(positions.Any(po => po.ClientOrderId == notification.Position.ClientOrderId))
            {
                var position = positions.FirstOrDefault(po => po.ClientOrderId == notification.Position.ClientOrderId);
                
                if(position != null)
                {
                    position.IsWorking = notification.Position.IsWorking;
                    position.QuantityFilled = notification.Position.QuantityFilled;
                    position.QuoteQuantityFilled = notification.Position.QuoteQuantityFilled;
                    position.Status = (OrderStatus)notification.Position.Status;
                }
            }
        }
    }
}