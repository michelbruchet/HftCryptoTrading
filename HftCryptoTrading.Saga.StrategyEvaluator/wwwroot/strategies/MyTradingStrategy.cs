public class MyTradingStrategy : IStrategy
{
    public string? Message { get; private set; }

    public string StrategyName => "MyTradingStrategy";

    public string Description => "This strategy buys-to-open (BTO) one share when the Stoch RSI (%K) is below 20 and crosses above the Signal (%D). Conversely, it sells-to-close (STC) and sells-to-open (STO) when the Stoch RSI is above 80 and crosses below the Signal.";

    public StrategyType StrategyType => StrategyType.General;

    public int Priority => 100;

    public ActionStrategy Execute(IEnumerable<Quote> quotes, params object[] parameters)
    {
        List<Quote> quotesList = quotes.OrderBy(q => q.Date).ToList();

        if (quotesList.Count < 14 * 3)
            return ActionStrategy.Error;

        List<StochRsiResult> resultsList =
          quotesList
          .GetStochRsi(14, 14, 3, 1)
          .ToList();

        return ActionStrategy.Short;
    }
}
