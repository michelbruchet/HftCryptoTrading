using HftCryptoTrading.Shared.Models;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Events;

[MessagePackObject]
public class ShortTradeSymbolDetectedEvent
{
    [Key(0)]
    public SymbolAnalysedSuccessFullyEvent Event { get; set; }
    [Key(1)]
    public List<KlineData> History { get; set; }
}