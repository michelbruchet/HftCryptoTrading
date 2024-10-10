using Skender.Stock.Indicators;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Templates;

public class MyIndicator : IIndicator
{
    public IEnumerable<decimal> Execute(IEnumerable<Quote> quotes, params object[] parameters)
    {
        Random random = new System.Random();
        return quotes.Select(q => (decimal)(random.NextDouble() * 100));
    }
}
