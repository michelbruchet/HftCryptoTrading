using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.Commands;

using System.Reactive;
using System.Threading.Tasks;
using HftCryptoTrading.Services.Commands;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class AnalysePriceChangeDetectedCommandTests
{
    private readonly Mock<IDistributedCache> _cacheServiceMock;
    private readonly Mock<ILogger<AnalysePriceChangeDetectedCommand>> _loggerMock;
    private readonly Mock<IMetricService> _metricServiceMock;
    private readonly Mock<IMarketWatcherSaga> _sagaMock;
    private readonly Mock<ISymbolAnalysisHelper> _SymbolAnalysisHelper;

    private readonly AnalysePriceChangeDetectedCommand _command;

    public AnalysePriceChangeDetectedCommandTests()
    {
        _cacheServiceMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<AnalysePriceChangeDetectedCommand>>();
        _metricServiceMock = new Mock<IMetricService>();
        _sagaMock = new Mock<IMarketWatcherSaga>();
        _SymbolAnalysisHelper = new Mock<ISymbolAnalysisHelper>();

        _SymbolAnalysisHelper.Setup(x => x.RetryPolicyWrapper(It.IsAny<Func<Task<bool>>>(), It.IsAny<int>()))
            .Returns((Func<Task<bool>> func, int retryCount) => func());

        _command = new AnalysePriceChangeDetectedCommand(
            _cacheServiceMock.Object,
            _loggerMock.Object,
            _metricServiceMock.Object,
            _sagaMock.Object,
            _SymbolAnalysisHelper.Object
        );
    }

    [Fact]
    public async Task RunAsync_ShouldPublishAnalysisResults_WhenAnomaliesDetected()
    {
        Random rdm = new Random();

        // Arrange
        var symbol = new SymbolTickerData("Test", new SymbolData("TestBNB"))
        {
            BookPrice = new BookPriceData("TestBNB")
            {
                BestAskPrice = (decimal)rdm.NextDouble(),
                BestAskQuantity = (decimal)rdm.NextDouble(),
                BestBidPrice = (decimal)rdm.NextDouble(),
                BestBidQuantity = (decimal)rdm.NextDouble(),
            },
            PriceChangePercent = (decimal)rdm.Next(1, 100),
            PublishedDate = DateTime.UtcNow,
            Ticker = new TickerData("TestBNB", "Test")
            {
                Ask = (decimal)rdm.NextDouble(),
                Bid = (decimal)rdm.NextDouble(),
                ChangePercentage = (decimal)rdm.Next(1, 100),
                HighPrice = (decimal)rdm.NextDouble(),
                LastPrice = (decimal)rdm.NextDouble(),
                LowPrice = (decimal)rdm.NextDouble(),
                Volume = (decimal)rdm.NextDouble(),
            }
        };

        MockHelperMethods(true, true, true);

        // Act
        await _command.RunAsync(symbol);

        // Assert
        _sagaMock.Verify(s => s.PublishStreamedSymbolAnalysedVolumeFailed(It.IsAny<string>(), It.IsAny<SymbolTickerData>()), Times.Once);
        _sagaMock.Verify(s => s.PublishStreamedSymbolAnalysedPriceFailed(It.IsAny<string>(), It.IsAny<SymbolTickerData>()), Times.Once);
        _sagaMock.Verify(s => s.PublishStreamedSymbolAnalysedSpreadBidAskFailed(It.IsAny<string>(), It.IsAny<SymbolTickerData>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldPublishAnalysisResults_WhenNoAnomaliesDetected()
    {
        Random rdm = new Random();

        // Arrange
        var symbol = new SymbolTickerData("Test", new SymbolData("TestBNB"))
        {
            BookPrice = new BookPriceData("TestBNB")
            {
                BestAskPrice = (decimal)rdm.NextDouble(),
                BestAskQuantity = (decimal)rdm.NextDouble(),
                BestBidPrice = (decimal)rdm.NextDouble(),
                BestBidQuantity = (decimal)rdm.NextDouble(),
            },
            PriceChangePercent = (decimal)rdm.Next(1, 100),
            PublishedDate = DateTime.UtcNow,
            Ticker = new TickerData("TestBNB", "Test")
            {
                Ask = (decimal)rdm.NextDouble(),
                Bid = (decimal)rdm.NextDouble(),
                ChangePercentage = (decimal)rdm.Next(1, 100),
                HighPrice = (decimal)rdm.NextDouble(),
                LastPrice = (decimal)rdm.NextDouble(),
                LowPrice = (decimal)rdm.NextDouble(),
                Volume = (decimal)rdm.NextDouble(),
            }
        };

        MockHelperMethods(false, false, false);

        // Act
        await _command.RunAsync(symbol);

        // Assert
        _sagaMock.Verify(s => s.PublishStreamedSymbolAnalysedVolumeFailed(It.IsAny<string>(), It.IsAny<SymbolTickerData>()), Times.Never);
        _sagaMock.Verify(s => s.PublishStreamedSymbolAnalysedPriceFailed(It.IsAny<string>(), It.IsAny<SymbolTickerData>()), Times.Never);
        _sagaMock.Verify(s => s.PublishStreamedSymbolAnalysedSpreadBidAskFailed(It.IsAny<string>(), It.IsAny<SymbolTickerData>()), Times.Never);
        _sagaMock.Verify(s => s.PublishStreamedSymbolAnalysedSuccessFully(It.IsAny<string>(), It.IsAny<SymbolTickerData>()), Times.Once);

        VerifyLogMessage("Valid symbol price changed detected for TestBNB");
    }


    private void MockHelperMethods(bool volumeResult, bool priceResult, bool spreadResult)
    {
        _SymbolAnalysisHelper.Setup(s => s.IsAbnormalVolume(It.IsAny<SymbolTickerData>())).ReturnsAsync(volumeResult);
        _SymbolAnalysisHelper.Setup(s => s.IsAbnormalPrice(It.IsAny<SymbolTickerData>())).ReturnsAsync(priceResult);
        _SymbolAnalysisHelper.Setup(s => s.IsAbnormalSpreadBidAsk(It.IsAny<SymbolTickerData>())).ReturnsAsync(spreadResult);
    }

    private void VerifyLogMessage(string expectedMessage)
    {
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(level => level == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString().Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}