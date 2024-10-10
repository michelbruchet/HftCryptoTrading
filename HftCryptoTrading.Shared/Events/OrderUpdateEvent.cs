using HftCryptoTrading.Shared.Models;
using MediatR;

namespace HftCryptoTrading.Shared.Events;

public class OrderUpdateEvent : INotification
{
    public OrderUpdate Position { get; private set; }
    public string Exchange { get; private set; }
    public string Symbol { get; private set; }

    public OrderUpdateEvent(string exchange, string symbol, OrderUpdate position)
    {
        Exchange = exchange;
        Symbol = symbol;
        Position = position;
    }
}

public class AccountBalanceUpdateEvent : INotification
{
    public AccountBalanceUpdate AccountBalance { get; private set; }
    public string Exchange { get; private set; }
    public string Symbol { get; private set; }

    public AccountBalanceUpdateEvent(string exchange, string symbol, AccountBalanceUpdate accountBalance)
    {
        AccountBalance = accountBalance;
        Exchange = exchange;
        Symbol = symbol;
    }
}
