using HftCryptoTrading.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Events;

public class ShortSymbolDetected
{
    public SymbolAnalysePriceEvent Symbol { get; set; }
    public List<KlineData> History { get; set; }
}
