using HftCryptoTrading.Shared.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Exchanges.Core.Events;

public class NewSymbolTickerDataEvent(string exchangeName, IEnumerable<SymbolTickerData> data):INotification
{
    public string ExchangeName => exchangeName;
    public IEnumerable<SymbolTickerData> Data => data;
}
