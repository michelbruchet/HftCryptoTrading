using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Services.Processes;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.Commands;

public class DownloadSymbolCommandTests
{
    private readonly Mock<IExchangeClient> _mockExchangeClient;
    private readonly Mock<IMetricService> _mockMetricService;
    private readonly Mock<IMarketWatcherSaga> _mockSaga;
    private readonly AppSettings _appSettings;

    public DownloadSymbolCommandTests()
    {
        _mockExchangeClient = new Mock<IExchangeClient>();
        _mockMetricService = new Mock<IMetricService>();
        _mockSaga = new Mock<IMarketWatcherSaga>();
        _appSettings = new AppSettings { LimitSymbolsMarket = 10 }; // Ajustez selon vos besoins
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDownloadSymbolsAndPublishSuccessfully()
    {
        // Arrange
        var symbols = new List<SymbolData> { new SymbolData { Name = "BTC" } };
        var tickers = new List<TickerData> { new TickerData("BTCUSDT", "Binance") { ChangePercentage = 1, Volume = 100 } };

        _mockExchangeClient.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(symbols);
        _mockExchangeClient.Setup(e => e.GetCurrentTickersAsync()).ReturnsAsync(tickers);
        _mockSaga.Setup(s => s.PublishDownloadedSymbols(It.IsAny<string>(), It.IsAny<IEnumerable<SymbolTickerData>>())).Returns(Task.CompletedTask);

        var _command = new DownloadSymbolCommand(_mockExchangeClient.Object, _mockMetricService.Object, _mockSaga.Object, _appSettings);

        // Act
        await _command.ExecuteAsync(CancellationToken.None);

        // Assert
        _mockMetricService.Verify(m => m.TrackSuccess("Download-symbols"), Times.Once);
        _mockMetricService.Verify(m => m.TrackSuccess("Download-tickers"), Times.Once);
        _mockMetricService.Verify(m => m.TrackSuccess("publish-symbols"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackFailureWhenSymbolsDownloadFails()
    {
        // Arrange
        _mockExchangeClient.Setup(e => e.GetSymbolsAsync()).ThrowsAsync(new Exception("Failed to download symbols"));
        var _command = new DownloadSymbolCommand(_mockExchangeClient.Object, _mockMetricService.Object, _mockSaga.Object, _appSettings);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _command.ExecuteAsync(CancellationToken.None));
        _mockMetricService.Verify(m => m.TrackFailure("Download-symbols", It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackFailureWhenTickersDownloadFails()
    {
        // Arrange
        var symbols = new List<SymbolData> { new SymbolData { Name = "BTC" } };
        _mockExchangeClient.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(symbols);
        _mockExchangeClient.Setup(e => e.GetCurrentTickersAsync()).ThrowsAsync(new Exception("Failed to download tickers"));

        var _command = new DownloadSymbolCommand(_mockExchangeClient.Object, _mockMetricService.Object, _mockSaga.Object, _appSettings);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _command.ExecuteAsync(CancellationToken.None));
        _mockMetricService.Verify(m => m.TrackFailure("Download-tickers", It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackFailureWhenPublishingSymbolsFails()
    {
        // Arrange
        var symbols = new List<SymbolData> { new SymbolData { Name = "BTC" } };
        var tickers = new List<TickerData> { new TickerData ("BTCUSDT", "Binance") { ChangePercentage = 1, Volume = 100 } };

        _mockExchangeClient.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(symbols);
        _mockExchangeClient.Setup(e => e.GetCurrentTickersAsync()).ReturnsAsync(tickers);

        _mockSaga.Setup(s => s.PublishDownloadedSymbols(It.IsAny<string>(), It.IsAny<IEnumerable<SymbolTickerData>>()))
            .ThrowsAsync(new Exception("Failed to publish symbols"));

        var _command = new DownloadSymbolCommand(_mockExchangeClient.Object, _mockMetricService.Object, _mockSaga.Object, _appSettings);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _command.ExecuteAsync(CancellationToken.None));
        _mockMetricService.Verify(m => m.TrackFailure("publish-symbols", It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackFailureWhenSymbolsAreNull()
    {
        // Arrange
        _mockExchangeClient.Setup(e => e.GetSymbolsAsync()).ReturnsAsync((List<SymbolData>)null);

        // Act
        var _command = new DownloadSymbolCommand(_mockExchangeClient.Object, _mockMetricService.Object, _mockSaga.Object, _appSettings);

        await _command.ExecuteAsync(CancellationToken.None);

        // Assert
        _mockMetricService.Verify(m => m.TrackFailure("Download-symbols"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackFailureWhenTickersAreNull()
    {
        // Arrange
        var symbols = new List<SymbolData> { new SymbolData { Name = "BTC" } };
        _mockExchangeClient.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(symbols);
        _mockExchangeClient.Setup(e => e.GetCurrentTickersAsync()).ReturnsAsync((List<TickerData>)null);

        // Act
        var _command = new DownloadSymbolCommand(_mockExchangeClient.Object, _mockMetricService.Object, _mockSaga.Object, _appSettings);
        await _command.ExecuteAsync(CancellationToken.None);

        // Assert
        _mockMetricService.Verify(m => m.TrackFailure("Download-tickers"), Times.Once);
    }
}