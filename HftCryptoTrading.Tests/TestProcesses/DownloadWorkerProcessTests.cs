using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.TestProcesses;

using Moq;
using Xunit;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.MarketDownloader.Processes;
using HftCryptoTrading.Saga.MarketDownloader.Workers;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared;

public class DownloadWorkerProcessTests
{
    private readonly Mock<IExchangeClient> _exchangeMock;
    private readonly Mock<IMetricService> _metricServiceMock;
    private readonly Mock<IMarketDownloaderSaga> _sagaMock;
    private readonly AppSettings _appSettings;
    private readonly Mock<IDisposable> _disposableMock;
    private readonly DownloadWorkerProcess _process;

    public DownloadWorkerProcessTests()
    {
        _exchangeMock = new Mock<IExchangeClient>();
        _metricServiceMock = new Mock<IMetricService>();
        _sagaMock = new Mock<IMarketDownloaderSaga>();
        _appSettings = new AppSettings { LimitSymbolsMarket = 5 };
        _disposableMock = new Mock<IDisposable>();

        _metricServiceMock.Setup(m => m.StartTracking(It.IsAny<string>()))
            .Returns(_disposableMock.Object);

        _process = new DownloadWorkerProcess(_exchangeMock.Object, _metricServiceMock.Object, _sagaMock.Object, _appSettings);
    }

    [Fact]
    public async Task DownloadSymbols_ShouldDownloadSymbolsAndTrackSuccess()
    {
        // Arrange
        var symbols = new List<SymbolData> { new SymbolData("BTC")};
        var tickers = new List<TickerData> { new TickerData("BTC"){PriceChangePercent = 2, Volume = 100 } };

        _exchangeMock.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(symbols);
        _exchangeMock.Setup(e => e.GetCurrentTickersAsync()).ReturnsAsync(tickers);

        // Act
        await _process.DownloadSymbols();

        // Assert
        _exchangeMock.Verify(e => e.GetSymbolsAsync(), Times.Once);
        _exchangeMock.Verify(e => e.GetCurrentTickersAsync(), Times.Once);
        _sagaMock.Verify(s => s.PublishSymbols(It.IsAny<string>(), It.IsAny<IEnumerable<SymbolTickerData>>()), Times.Once);
        _metricServiceMock.Verify(m => m.TrackSuccess("Download-symbols"), Times.Once);
        _metricServiceMock.Verify(m => m.TrackSuccess("Download-tickers"), Times.Once);
        _metricServiceMock.Verify(m => m.TrackSuccess("publish-symbols"), Times.Once);
    }

    [Fact]
    public async Task DownloadSymbols_ShouldHandleDownloadSymbolsFailure()
    {
        // Arrange
        var exception = new Exception("Test exception");

        _exchangeMock.Setup(e => e.GetSymbolsAsync()).ThrowsAsync(exception);

        // Act
        await Assert.ThrowsAsync<Exception>(() => _process.DownloadSymbols());

        // Assert
        _exchangeMock.Verify(e => e.GetSymbolsAsync(), Times.Exactly(4)); // Retry 3 times
        _metricServiceMock.Verify(m => m.TrackFailure("Download-symbols", exception), Times.Once);
    }

    [Fact]
    public async Task DownloadSymbols_ShouldHandleGetTickersFailure()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var symbols = new List<SymbolData> { new SymbolData("BTC") };

        _exchangeMock.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(symbols);
        _exchangeMock.Setup(e => e.GetCurrentTickersAsync()).ThrowsAsync(exception);

        // Act
        await Assert.ThrowsAsync<Exception>(() => _process.DownloadSymbols());

        // Assert
        _exchangeMock.Verify(e => e.GetCurrentTickersAsync(), Times.Exactly(4)); // Retry 3 times
        _metricServiceMock.Verify(m => m.TrackFailure("Download-tickers", exception), Times.Once);
    }


    [Fact]
    public async Task DownloadSymbols_ShouldHandlePublishFailure()
    {
        // Arrange
        var symbols = new List<SymbolData> { new SymbolData ("BTC") };
        var tickers = new List<TickerData> { new TickerData ("BTC") { PriceChangePercent = 2, Volume = 100 } };
        var exception = new Exception("Test exception");

        _exchangeMock.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(symbols);
        _exchangeMock.Setup(e => e.GetCurrentTickersAsync()).ReturnsAsync(tickers);
        _sagaMock.Setup(s => s.PublishSymbols(It.IsAny<string>(), It.IsAny<IEnumerable<SymbolTickerData>>())).ThrowsAsync(exception);

        // Act
        await Assert.ThrowsAsync<Exception>(() => _process.DownloadSymbols());

        // Assert
        _sagaMock.Verify(s => s.PublishSymbols(It.IsAny<string>(), It.IsAny<IEnumerable<SymbolTickerData>>()), Times.Exactly(4)); // Retry 3 times
        _metricServiceMock.Verify(m => m.TrackFailure("publish-symbols", exception), Times.Once);
    }
}
