using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.Commands;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using HftCryptoTrading.Services.Processes;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using MessagePack;
using HftCryptoTrading.Services.Commands;

public class AnalyseDownloadedSymbolCommandTests
{
    private readonly Mock<IDistributedCache> _mockCacheService;
    private readonly Mock<ILogger<AnalyseDownloadedSymbolCommand>> _mockLogger;
    private readonly Mock<IMetricService> _mockMetricService;
    private readonly Mock<IMarketWatcherSaga> _mockSaga;
    private readonly SymbolAnalysisHelper _symbolAnalysisHelper;

    private const string VolumeHistoryKeyPrefix = "VolumeHistory_";
    private const string SpreadHistoryKeyPrefix = "SpreadHistory_";
    private const string PriceHistoryKeyPrefix = "PriceHistory_";

    public AnalyseDownloadedSymbolCommandTests()
    {
        _mockCacheService = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<AnalyseDownloadedSymbolCommand>>();
        _mockMetricService = new Mock<IMetricService>();
        _mockSaga = new Mock<IMarketWatcherSaga>();
        _symbolAnalysisHelper = new SymbolAnalysisHelper(_mockCacheService.Object, _mockLogger.Object, 
            _mockMetricService.Object, _mockSaga.Object);
    }

    [Fact]
    public async Task RunAsync_WithAbnormalPrice()
    {
        // Arrange
        var command = new AnalyseDownloadedSymbolCommand(
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockSaga.Object,
            _mockMetricService.Object,
            _symbolAnalysisHelper
            );

        var abnormalPriceEventData = new PublishedDownloadedSymbolsEvent("TestExchange",
            new List<SymbolTickerData>
            {
                new SymbolTickerData("TestExchange", new SymbolData("BTCUSDT")
                {
                    BaseAsset = "USDT",
                    AllowTrailingStop=false,
                    BaseAssetPrecision=8,
                    BaseFeePrecision=3
                })
                {
                    Ticker = new TickerData("TestSymbol", "TestExchange") { LastPrice = 5000 , Volume = 100 },
                    BookPrice = new BookPriceData("TestSymbol") { BestBidPrice = 49, BestAskPrice = 51 }
                }
            });

        _mockCacheService.Setup(c => c.GetAsync($"{PriceHistoryKeyPrefix}{abnormalPriceEventData.Data.First().Symbol.Name}", 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(90m));

        // Act
        await command.RunAsync(abnormalPriceEventData);

        // Assert
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedPriceFailed("TestExchange", It.IsAny<List<SymbolTickerData>>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithAbnormalVolume()
    {
        // Arrange
        var command = new AnalyseDownloadedSymbolCommand(
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockSaga.Object,
            _mockMetricService.Object,
            _symbolAnalysisHelper
            );

        var abnormalVolumeEventData = new PublishedDownloadedSymbolsEvent("TestExchange",
            new List<SymbolTickerData>
            {
                new SymbolTickerData("TestExchange", new SymbolData("BTCUSDT")
                {
                    BaseAsset = "USDT",
                    AllowTrailingStop=false,
                    BaseAssetPrecision=8,
                    BaseFeePrecision=3
                })
                {
                    Ticker = new TickerData("TestSymbol", "TestExchange") { LastPrice = 50 , Volume = 100 },
                    BookPrice = new BookPriceData("TestSymbol") { BestBidPrice = 49, BestAskPrice = 51 }
                }
            });

        _mockCacheService.Setup(c => c.GetAsync($"{VolumeHistoryKeyPrefix}{abnormalVolumeEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(30m));



        // Act
        await command.RunAsync(abnormalVolumeEventData);

        // Assert
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedVolumeFailed("TestExchange", abnormalVolumeEventData.Data.ToList()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithAbnormalSpreadBidAsk()
    {
        // Arrange
        var command = new AnalyseDownloadedSymbolCommand(
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockSaga.Object,
            _mockMetricService.Object,
            _symbolAnalysisHelper
            );

        var abnormalSpreadBidAskEventData = new PublishedDownloadedSymbolsEvent("TestExchange",
            new List<SymbolTickerData>
            {
                new SymbolTickerData("TestExchange", new SymbolData("BTCUSDT")
                {
                    BaseAsset = "USDT",
                    AllowTrailingStop=false,
                    BaseAssetPrecision=8,
                    BaseFeePrecision=3
                })
                {
                    Ticker = new TickerData("TestSymbol", "TestExchange") { LastPrice = 50 , Volume = 100 },
                    BookPrice = new BookPriceData("TestSymbol") { BestBidPrice = 49, BestAskPrice = 51 }
                }
            });

        _mockCacheService.Setup(c => c.GetAsync($"{SpreadHistoryKeyPrefix}{abnormalSpreadBidAskEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(30m));

        // Act
        await command.RunAsync(abnormalSpreadBidAskEventData);

        // Assert
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSpreadBidAskFailed("TestExchange", abnormalSpreadBidAskEventData.Data.ToList()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithValidTicker()
    {
        // Arrange
        var command = new AnalyseDownloadedSymbolCommand(
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockSaga.Object,
            _mockMetricService.Object,
            _symbolAnalysisHelper
            );

        var validEventData = new PublishedDownloadedSymbolsEvent("TestExchange",
            new List<SymbolTickerData>
            {
                new SymbolTickerData("TestExchange", new SymbolData("BTCUSDT")
                {
                    BaseAsset = "USDT",
                    AllowTrailingStop=false,
                    BaseAssetPrecision=8,
                    BaseFeePrecision=3
                })
                {
                    Ticker = new TickerData("TestSymbol", "TestExchange") { LastPrice = 50 , Volume = 100 },
                    BookPrice = new BookPriceData("TestSymbol") { BestBidPrice = 49, BestAskPrice = 51 }
                }
            });

        _mockCacheService.Setup(c => c.GetAsync($"{PriceHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(45m));

        _mockCacheService.Setup(c => c.GetAsync($"{VolumeHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(90m));

        _mockCacheService.Setup(c => c.GetAsync($"{SpreadHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(2m));

        // Act
        await command.RunAsync(validEventData);

        // Assert
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSuccessFully("TestExchange", validEventData.Data.ToList()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WithInValidTicker()
    {
        // Arrange
        var command = new AnalyseDownloadedSymbolCommand(
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockSaga.Object,
            _mockMetricService.Object,
            _symbolAnalysisHelper
            );

        var validEventData = new PublishedDownloadedSymbolsEvent("TestExchange",
            new List<SymbolTickerData>
            {
                new SymbolTickerData("TestExchange", new SymbolData("BTCUSDT")
                {
                    BaseAsset = "USDT",
                    AllowTrailingStop=false,
                    BaseAssetPrecision=8,
                    BaseFeePrecision=3
                })
                {
                    Ticker = new TickerData("TestSymbol", "TestExchange") { LastPrice = 500 , Volume = 30 },
                    BookPrice = new BookPriceData("TestSymbol") { BestBidPrice = 49, BestAskPrice = 51 }
                }
            });

        _mockCacheService.Setup(c => c.GetAsync($"{PriceHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(35m));

        _mockCacheService.Setup(c => c.GetAsync($"{VolumeHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(150m));

        _mockCacheService.Setup(c => c.GetAsync($"{SpreadHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(MessagePackSerializer.Serialize(50m));

        // Act
        await command.RunAsync(validEventData);

        // Assert
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedPriceFailed("TestExchange", validEventData.Data.ToList()), Times.Once);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedVolumeFailed("TestExchange", validEventData.Data.ToList()), Times.Once);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSpreadBidAskFailed("TestExchange", validEventData.Data.ToList()), Times.Once);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSuccessFully("TestExchange", validEventData.Data.ToList()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WithInValidThenValidTicker()
    {
        // Arrange
        var command = new AnalyseDownloadedSymbolCommand(
            _mockCacheService.Object,
            _mockLogger.Object,
            _mockSaga.Object,
            _mockMetricService.Object,
            _symbolAnalysisHelper
            );

        var invalidEventData = new PublishedDownloadedSymbolsEvent("TestExchange",
            new List<SymbolTickerData>
            {
                new SymbolTickerData("TestExchange", new SymbolData("BTCUSDT")
                {
                    BaseAsset = "USDT",
                    AllowTrailingStop=false,
                    BaseAssetPrecision=8,
                    BaseFeePrecision=3
                })
                {
                    Ticker = new TickerData("TestSymbol", "TestExchange") { LastPrice = 500 , Volume = 30 },
                    BookPrice = new BookPriceData("TestSymbol") { BestBidPrice = 49, BestAskPrice = 51 }
                }
            });

        decimal previousPrice = 35m;
        decimal previousVolume = 150m;
        decimal previousSpread = 50m;

        _mockCacheService.Setup(c => c.GetAsync($"{PriceHistoryKeyPrefix}{invalidEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(()=> MessagePackSerializer.Serialize(previousPrice));

        _mockCacheService.Setup(c => c.GetAsync($"{VolumeHistoryKeyPrefix}{invalidEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(()=>MessagePackSerializer.Serialize(previousVolume));

        _mockCacheService.Setup(c => c.GetAsync($"{SpreadHistoryKeyPrefix}{invalidEventData.Data.First().Symbol.Name}",
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(()=>MessagePackSerializer.Serialize(previousSpread));

        // Act
        await command.RunAsync(invalidEventData);

        // Assert
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedPriceFailed("TestExchange", invalidEventData.Data.ToList()), Times.Once);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedVolumeFailed("TestExchange", invalidEventData.Data.ToList()), Times.Once);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSpreadBidAskFailed("TestExchange", invalidEventData.Data.ToList()), Times.Once);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSuccessFully("TestExchange", invalidEventData.Data.ToList()), Times.Never);
     
        _mockCacheService.Verify(c => c.SetAsync($"{PriceHistoryKeyPrefix}{invalidEventData.Data.First().Symbol.Name}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()
            ), Times.Never);
        
        _mockCacheService.Verify(c => c.SetAsync($"{VolumeHistoryKeyPrefix}{invalidEventData.Data.First().Symbol.Name}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()
            ), Times.Never);
        
        _mockCacheService.Verify(c => c.SetAsync($"{SpreadHistoryKeyPrefix}{invalidEventData.Data.First().Symbol.Name}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()
            ), Times.Never);

        var validEventData = new PublishedDownloadedSymbolsEvent("TestExchange",
            new List<SymbolTickerData>
            {
                new SymbolTickerData("TestExchange", new SymbolData("BTCUSDT")
                {
                    BaseAsset = "USDT",
                    AllowTrailingStop=false,
                    BaseAssetPrecision=8,
                    BaseFeePrecision=3
                })
                {
                    Ticker = new TickerData("TestSymbol", "TestExchange") { LastPrice = previousPrice * 1.05m , Volume = previousVolume * 1.05m },
                    BookPrice = new BookPriceData("TestSymbol") { BestBidPrice = 50, BestAskPrice = 90 }
                }
            });

        await command.RunAsync(validEventData);

        // Assert
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedPriceFailed("TestExchange", validEventData.Data.ToList()), Times.Never);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedVolumeFailed("TestExchange", validEventData.Data.ToList()), Times.Never);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSpreadBidAskFailed("TestExchange", validEventData.Data.ToList()), Times.Never);
        _mockSaga.Verify(s => s.PublishDownloadedSymbolAnalysedSuccessFully("TestExchange", validEventData.Data.ToList()), Times.Once);

        _mockCacheService.Verify(c => c.SetAsync($"{PriceHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()
            ), Times.Once);

        _mockCacheService.Verify(c => c.SetAsync($"{VolumeHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()
            ), Times.Once);

        _mockCacheService.Verify(c => c.SetAsync($"{SpreadHistoryKeyPrefix}{validEventData.Data.First().Symbol.Name}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()
            ), Times.Once);
    }
}