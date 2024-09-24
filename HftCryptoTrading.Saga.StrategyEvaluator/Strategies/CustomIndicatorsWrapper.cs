using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
using Skender.Stock.Indicators;

namespace HftCryptoTrading.Saga.StrategyEvaluator.Strategies;

public static class CustomIndicatorWrapper
{
    // Custom Indicator wrapper
    public static IEnumerable<decimal> CustomIndicator(
      this IEnumerable<Quote> quotes,
      string name,
      params object[] parameters)
    {
        return IndicatorLoaderService.Service.ExecuteIndicator(name, quotes, parameters);
    }
}
