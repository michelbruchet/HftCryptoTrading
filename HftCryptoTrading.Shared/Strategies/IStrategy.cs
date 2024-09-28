using Skender.Stock.Indicators;

namespace HftCryptoTrading.Shared.Strategies;

public enum StrategyType:int
{
    Customer=300,
    Server=200,
    General=100
}

public enum ActionStrategy
{
    Long,
    Short,
    Hold,
    Error
}
public interface IStrategy
{
    ActionStrategy Execute(IEnumerable<Quote> quotes, params object[] parameters);
    string Message { get; }
    string StrategyName { get; }
    string Description { get; }
    StrategyType StrategyType { get; }
    int Priority { get; }
}
