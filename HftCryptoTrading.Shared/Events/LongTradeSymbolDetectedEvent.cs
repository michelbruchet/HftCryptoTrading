using HftCryptoTrading.Shared.Models;
using MessagePack;

namespace HftCryptoTrading.Shared.Events;

[MessagePackObject]
public class LongTradeSymbolDetectedEvent
{
    [Key(0)]
    public SymbolAnalysedSuccessFullyEvent Event { get; set; }
    [Key(1)]
    public List<KlineData> History { get; set; }
}
