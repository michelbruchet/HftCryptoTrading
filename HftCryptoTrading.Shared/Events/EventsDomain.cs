using HftCryptoTrading.Shared.Models;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Events;

[MessagePackObject]
public record class NewSymbolPublishedEvent()
{
    public NewSymbolPublishedEvent(Guid id, IEnumerable<SymbolTickerData> data, DateTime publishedDate):this()
    {
        Id = id;
        PublishedDate = publishedDate;
        Data = data;
    }

    public NewSymbolPublishedEvent(IEnumerable<SymbolTickerData> data):this(Guid.NewGuid(), data, DateTime.UtcNow)
    {
    }

    public NewSymbolPublishedEvent(IEnumerable<SymbolTickerData> data, DateTime publishedDate) : this(Guid.NewGuid(), data, publishedDate)
    {
    }

    [Key(0)]
    public Guid Id { get; set; }

    [Key(1)]
    public DateTime PublishedDate { get; set; }

    [Key(2)]
    public IEnumerable<SymbolTickerData> Data { get; set; } = default!;
}