using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Strategies;
using Microsoft.AspNetCore.Routing;
using Moq;
using Skender.Stock.Indicators;
using StrategyExecution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.Strategies;

public class StrategyLoaderServiceTest
{
    private Mock<IMetricService> _metricServiceMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;

    public StrategyLoaderServiceTest()
    {
        _metricServiceMock = new Mock<IMetricService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

        // Mock service scope and metric service
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);

        // Mock service provider to return IMetricService and IServiceScopeFactory
        _serviceProviderMock.Setup(x => x.GetService(typeof(IMetricService)))
            .Returns(_metricServiceMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
    }

    [Fact]
    public void LoadStrategies_ValidPath_ShouldLoadStrategiesSuccessfully()
    {
        // Arrange
        var strategyLoaderService = new StrategyLoaderService(_metricServiceMock.Object);

        var path = new DirectoryInfo("strategies").FullName;

        if (Directory.Exists(path))
            Directory.Delete(path, true);

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
                        List<Quote> quotesList = quotes.OrderBy(q=>q.Date).ToList();

                        if(quotesList.Count < 14 *3)
                            return ActionStrategy.Error;

                        List<StochRsiResult> resultsList =
                          quotesList
                          .GetStochRsi(14, 14, 3, 1)
                          .ToList();

                        return ActionStrategy.Short;
                    }
                }");

        // Act
        strategyLoaderService.LoadStrategies(path);

        // Assert
        _metricServiceMock.Verify(m => m.TrackSuccess("LoadStrategies"), Times.Once);
        Assert.True(strategyLoaderService.Strategies.ContainsKey("strategy1"));
    }

    [Fact]
    public void LoadStrategies_InvalidPath_ShouldThrowDirectoryNotFoundException()
    {
        // Arrange
        var strategyLoaderService = new StrategyLoaderService(_metricServiceMock.Object);

        var invalidPath = "invalidPath";

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => strategyLoaderService.LoadStrategies(invalidPath));
        _metricServiceMock.Verify(m => m.TrackFailure("LoadStrategies"), Times.Once);
    }

    [Fact]
    public void CompileStrategy_ValidFile_ShouldCompileAndAddStrategy()
    {
        // Arrange
        var strategyLoaderService = new StrategyLoaderService(_metricServiceMock.Object);

        var path = new DirectoryInfo("strategies").FullName;

        if (Directory.Exists(path))
            Directory.Delete(path, true);

        // Simulate a directory and files
        Directory.CreateDirectory(path);

        var filePath = Path.Combine(path, "MyTradingStrategy.cs");

        File.WriteAllText(filePath, @"public class MyTradingStrategy : IStrategy
        {
            public string? Message { get; private set; }

            public string StrategyName => ""MyTradingStrategy"";

            public string Description => ""This strategy buys-to-open (BTO) one share when the Stoch RSI (%K) is below 20 and crosses above the Signal (%D). Conversely, it sells-to-close (STC) and sells-to-open (STO) when the Stoch RSI is above 80 and crosses below the Signal."";

            public StrategyType StrategyType => StrategyType.General;

            public int Priority => 100;

            public ActionStrategy Execute(IEnumerable<Quote> quotes, params object[] parameters)
            {
                List<Quote> quotesList = quotes.OrderBy(q=>q.Date).ToList();

                if(quotesList.Count < 14 *3)
                    return ActionStrategy.Error;

                List<StochRsiResult> resultsList =
                  quotesList
                  .GetStochRsi(14, 14, 3, 1)
                  .ToList();

                return ActionStrategy.Short;
            }
        }");

        // Act
        strategyLoaderService.AddStrategy(filePath);

        // Assert
        Assert.True(strategyLoaderService.Strategies.ContainsKey("MyTradingStrategy"));
        _metricServiceMock.Verify(m => m.TrackSuccess("CompileStrategy"), Times.Once);
    }

    [Fact]
    public void AddStrategy_FileNotFound_ShouldThrowFileNotFoundException()
    {
        var path = new DirectoryInfo("strategies").FullName;
        var strategyLoaderService = new StrategyLoaderService(_metricServiceMock.Object);

        if (Directory.Exists(path))
            Directory.Delete(path, true);

        // Simulate a directory and files
        Directory.CreateDirectory(path);

        var filePath = Path.Combine(path, "MyTradingStrategy.cs");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => strategyLoaderService.AddStrategy(filePath));
        _metricServiceMock.Verify(m => m.TrackFailure("AddStrategy"), Times.Once);
    }

    [Fact]
    public void Evaluate_MultipleStrategies_ShouldReturnBestActionStrategy()
    {
        var strategyLoaderService = new StrategyLoaderService(_metricServiceMock.Object);

        // Adding mock strategies
        strategyLoaderService.AddStrategy("strategy1", new Mock<IStrategy>().Object);
        strategyLoaderService.AddStrategy("strategy2", new Mock<IStrategy>().Object);

        var quotes = new List<Quote>();

        // Act
        var result = strategyLoaderService.Evaluate(quotes);

        // Assert
        Assert.NotEqual(result, ActionStrategy.Error);
        _metricServiceMock.Verify(m => m.TrackSuccess("Evaluate"), Times.Once);
    }
}