using Skender.Stock.Indicators;
using System.Collections.Generic;
using System.Linq;
using HftCryptoTrading.Shared.Strategies;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Templates;

public class MyIndicator : IIndicator
{
    public IEnumerable<decimal> Execute(IEnumerable<Quote> quotes, params object[] parameters)
    {
        Random random = new System.Random();
        return quotes.Select(q => (decimal)(random.NextDouble() * 100));
    }
}
