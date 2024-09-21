﻿using HftCryptoTrading.Shared.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Exchanges.Core.Events;

public class NewSymbolTickerDataEvent(IEnumerable<SymbolTickerData> data):INotification
{
    public IEnumerable<SymbolTickerData> Data => data;
}
