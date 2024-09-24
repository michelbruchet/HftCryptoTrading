using Skender.Stock.Indicators;

namespace HftCryptoTrading.Shared.Strategies;

public interface IIndicator
{
    IEnumerable<decimal> Execute(IEnumerable<Quote> quotes, params object[] parameters);
}