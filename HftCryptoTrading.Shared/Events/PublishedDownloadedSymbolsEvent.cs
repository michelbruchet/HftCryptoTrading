using HftCryptoTrading.Shared.Models;
using MediatR;

namespace HftCryptoTrading.Shared.Events;

public class PublishedDownloadedSymbolsEvent(string exchangeName, IEnumerable<SymbolTickerData> data) : IRequest
{
    public string ExchangeName => exchangeName;
    public IEnumerable<SymbolTickerData> Data => data;
}
