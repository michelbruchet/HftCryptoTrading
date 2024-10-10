using HftCryptoTrading.Shared.Strategies;

namespace HftCryptoTrading.Shared.Models;

public class PlaceOrder(string symbol, string exchange, ActionStrategy strategyAction)
{
    public string Symbol { get; private set; } = symbol;
    public string Exchange { get; private set; } = exchange;
    public ActionStrategy Strategy { get; private set; } = strategyAction;
    public decimal Quantity { get; set; }
    public string? ClientOrderId { get; set; }
}
