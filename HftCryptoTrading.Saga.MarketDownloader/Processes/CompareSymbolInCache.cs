using HftCryptoTrading.Shared.Models;
using System.Diagnostics.CodeAnalysis;

namespace HftCryptoTrading.Saga.MarketDownloader.Processes;

public class CompareSymbolInCache : IEqualityComparer<SymbolData>
{
    public bool Equals(SymbolData? x, SymbolData? y)
    {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return x.Symbol.Equals(y.Symbol);
    }

    public int GetHashCode([DisallowNull] SymbolData obj)
    {
        return obj.Symbol.GetHashCode();
    }
}
