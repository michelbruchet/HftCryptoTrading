using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HftCryptoTrading.Services.Processes;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Skender.Stock.Indicators;
using StrategyExecution;
using HftCryptoTrading.Shared.Strategies;
using CryptoExchange.Net.CommonObjects;

namespace HftCryptoTrading.Tests.Commands;


public class EvaluateStrategyCommandTests
{
    private Mock<IMetricService> _metricServiceMock = new();
    private LoggerFactory _loggerFactory;
    private Mock<IStrategyAnalyserSaga> _strategySaga = new();

    public EvaluateStrategyCommandTests()
    {
        _loggerFactory = new LoggerFactory();
    }

    [Fact]
    public async Task Evaluate_ShouldPublishStrategyResult_WhenNoExceptionIsThrown()
    {
        var cancellationToken = CancellationToken.None;
        var actionResult = ActionStrategy.Hold;

        var path = new DirectoryInfo("strategies").FullName;

        if (Directory.Exists(path))
            Directory.Delete(path, true);

        StrategyLoaderService StrategyLoaderService = new StrategyLoaderService(_metricServiceMock.Object);
        var command = new EvaluateStrategyCommand(_metricServiceMock.Object, _loggerFactory.CreateLogger<EvaluateStrategyCommand>(), _strategySaga.Object, StrategyLoaderService);

        // Simulate a directory and files
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "strategy1.cs"),
            @"public class MyTradingStrategy : IStrategy
                {
                    public string? Message { get; private set; }
                    public string StrategyName => ""MyTradingStrategy"";
                    public string Description => ""This strategy buys-to-open (BTO) one share when the Stoch RSI (%K) is below 20 and crosses above the Signal (%D). Conversely, it sells-to-close (STC) and sells-to-open (STO) when the Stoch RSI is above 80 and crosses below the Signal."";
                    public StrategyType StrategyType => StrategyType.General;
                    public int Priority => 100;

                    public ActionStrategy Execute(IEnumerable<Quote> quotes, params object[] parameters)
                    {
                        return ActionStrategy.Hold;
                    }
                }");

        DownloadSymbolHistoryEvent notification = CreateSimpleEventData();

        // Act
        StrategyLoaderService.LoadStrategies(path);

        await command.Evaluate(notification, cancellationToken);

        // Assert
        _strategySaga.Verify(s => s.PublishStrategyResult(actionResult, notification), Times.Once);
    }

    static Random rdm = new Random();

    [Fact]
    public async Task Evaluate_ShouldLogErrorAndTrackFailure_WhenExceptionIsThrown()
    {
        try
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var actionResult = ActionStrategy.Hold;

            var StrategyLoaderService = new StrategyLoaderService(_metricServiceMock.Object);

            var path = new DirectoryInfo($"strategies_{DateTime.UtcNow.ToString("yyyMMddHHmmss")}").FullName;

            if (Directory.Exists(path))
                Directory.Delete(path, true);

            // Simulate a directory and files
            Directory.CreateDirectory(path);

            File.WriteAllText(Path.Combine(path, $"strategy_{DateTime.UtcNow.ToString("yyyMMddHHmmss")}.cs"),
                @"public class MyTradingStrategy : IStrategy
                {
                    public string? Message { get; private set; }
                    public string StrategyName => ""MyTradingStrategy"";
                    public string Description => ""This strategy buys-to-open (BTO) one share when the Stoch RSI (%K) is below 20 and crosses above the Signal (%D). Conversely, it sells-to-close (STC) and sells-to-open (STO) when the Stoch RSI is above 80 and crosses below the Signal."";
                    public StrategyType StrategyType => StrategyType.General;
                    public int Priority => 100;

                    public ActionStrategy Execute(IEnumerable<Quote> quotes, params object[] parameters)
                    {
                        throw new Exception(""plateform not yet implemented"");
                    }
                }");

            // Act
            var command = new EvaluateStrategyCommand(_metricServiceMock.Object, _loggerFactory.CreateLogger<EvaluateStrategyCommand>(), _strategySaga.Object, 
                StrategyLoaderService);
            StrategyLoaderService.LoadStrategies(path);

            DownloadSymbolHistoryEvent notification = CreateSimpleEventData();

            // Act
            await Assert.ThrowsAnyAsync<Exception>(async () => await command.Evaluate(notification, cancellationToken));
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private static DownloadSymbolHistoryEvent CreateSimpleEventData()
    {
        var symbol = new SymbolData("BTCUSDT");

        var symbolAnalysed = new SymbolAnalysedSuccessFullyEvent("BTCUSDT", symbol)
        {
            PublishedDate = DateTime.UtcNow,
            Price = (decimal)rdm.NextDouble(),
            HighPrice = (decimal)rdm.NextDouble(),
            LowPrice = (decimal)rdm.NextDouble(),
            Volume = (decimal)rdm.NextDouble(),
            PriceChangePercent = (decimal)rdm.Next(1, 100)
        };

        var klines = Enumerable.Range(1, 500).Select(
            i => new KlineData("BTCUSDT")
            {
                OpenTime = DateTime.UtcNow.AddMinutes(-(i + 5)),
                OpenPrice = (decimal)rdm.NextDouble(),
                CloseTime = DateTime.UtcNow.AddMinutes(-i),
                ClosePrice = (decimal)rdm.NextDouble(),
                HighPrice = (decimal)rdm.NextDouble(),
                LowPrice = (decimal)rdm.NextDouble(),
                Volume = (decimal)rdm.NextDouble()
            }).OrderBy(k => k.OpenTime).ToList();

        var notification = new DownloadSymbolHistoryEvent(symbolAnalysed, klines);
        return notification;
    }
}