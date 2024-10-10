using HftCryptoTrading.Shared.Models;
using MediatR;

namespace HftCryptoTrading.Shared.Events;

public class PublishSymbolAnalysedSuccessFullyEvent() : IRequest
{
    public PublishSymbolAnalysedSuccessFullyEvent(string exchangeName, List<SymbolTickerData> validSymbols) : this()
    {
        ValidSymbols = validSymbols;
        ExchangeName = exchangeName;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public DateTime PublishedDate { get; } = DateTime.UtcNow;
    public List<SymbolTickerData> ValidSymbols { get; }
    public string ExchangeName { get; }
}
