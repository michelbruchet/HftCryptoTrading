using HftCryptoTrading.Shared.Models;
using MediatR;

namespace HftCryptoTrading.Shared.Events;

public class AccountPositionUpdateEvent : INotification
{
    public AccountPosition Position { get; private set; }
    public string Exchange { get; private set; }
    public string Symbol { get; private set; }

    public AccountPositionUpdateEvent(string exchange, string symbol, AccountPosition position)
    {
        Exchange = exchange;
        Symbol = symbol;
        Position = position;
    }
}
