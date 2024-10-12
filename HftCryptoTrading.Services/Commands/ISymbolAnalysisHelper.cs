using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HftCryptoTrading.Services.Commands
{
    public interface ISymbolAnalysisHelper
    {
        Task<T> GetFromCacheAsync<T>(string key);
        Task<bool> IsAbnormalPrice(SymbolTickerData symbol);
        Task<bool> IsAbnormalSpreadBidAsk(SymbolTickerData symbol);
        Task<bool> IsAbnormalVolume(SymbolTickerData symbol);

        Task<T> RetryPolicyWrapper<T>(Func<Task<T>> action, int maxRetries = 3);
        Task SetToCacheAsync<T>(string key, T value);
    }
}