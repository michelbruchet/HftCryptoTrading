using HftCryptoTrading.Saga.StrategyEvaluator.Strategies;
using HftCryptoTrading.Saga.StrategyEvaluator.Templates;
using HftCryptoTrading.Shared.Strategies;
using Moq;
using Skender.Stock.Indicators;
using StrategyExecution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Tests.Strategies;

public class StrategyLoaderServiceTests
{
    private readonly Mock<IStrategy> _mockStrategy;
    private readonly string _mockStrategyPath;

    private string GetStrategyCode(string strategyName) => $@"
                public class {strategyName} : IStrategy
                {{
                    public string Message {{get; private set;}}
                    public string StrategyName => ""{strategyName}"";
                    public int Priority => 100;
                    public string Description => @""It will buy - to - open(BTO) one share when the Stoch RSI(% K) is below 20 and crosses over the Signal
                                                        (% D).The reverse Sell - to - Close(STC) and Sell - To - Open(STO) occurs when the Stoch RSI is above 80 and crosses below the Signal."";
                    public StrategyType StrategyType => StrategyType.Server;
                    public ActionStrategy Execute(IEnumerable<Quote> quotes, params object[] parameters)
                    {{
                        return ActionStrategy.Short;
                    }}
                }}";

    private string GetStrategyCodeWithIndicator(string strategyName) => $@"
                public class {strategyName} : IStrategy
                {{
                    public string Message {{get; private set;}}
                    public string StrategyName => ""{strategyName}"";
                    public int Priority => 100;
                    public string Description => @""It will buy - to - open(BTO) one share when the Stoch RSI(% K) is below 20 and crosses over the Signal
                                                        (% D).The reverse Sell - to - Close(STC) and Sell - To - Open(STO) occurs when the Stoch RSI is above 80 and crosses below the Signal."";
                    public StrategyType StrategyType => StrategyType.Server;
                    public ActionStrategy Execute(IEnumerable<Quote> quotes, params object[] parameters)
                    {{
                        List<Quote> quotesList = quotes.OrderBy(q=>q.Date).ToList();

                        if(quotesList.Count < 14 *3)
                            return ActionStrategy.Error;

                        List<StochRsiResult> resultsList =
                          quotesList
                          .GetStochRsi(14, 14, 3, 1)
                          .ToList();

                        return ActionStrategy.Long;
                    }}
                }}";

    public StrategyLoaderServiceTests()
    {
        _mockStrategy = new Mock<IStrategy>();
        _mockStrategyPath = new DirectoryInfo("indicators").FullName;

        if (!Directory.Exists(_mockStrategyPath))
            Directory.CreateDirectory(_mockStrategyPath);
    }

    // Test: Check if singleton instance is created
    [Fact]
    public void Service_ShouldReturnSingletonInstance()
    {
        // Act
        var instance1 = StrategyLoaderService.Service;
        var instance2 = StrategyLoaderService.Service;

        // Assert
        Assert.Equal(instance1, instance2); // Should be the same instance
    }

    // Test: LoadStrategies throws DirectoryNotFoundException if the path doesn't exist
    [Fact]
    public void LoadStrategies_ShouldThrowDirectoryNotFoundException_WhenPathDoesNotExist()
    {
        // Arrange
        var service = StrategyLoaderService.Service;
        var nonExistentPath = "nonExistentPath";

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => service.LoadStrategies(nonExistentPath));
    }

    // Test: AddStrategy should throw FileNotFoundException if file doesn't exist
    [Fact]
    public void AddStrategy_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var service = StrategyLoaderService.Service;
        var nonExistentFile = "nonExistentFile.cs";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => service.AddStrategy(nonExistentFile));
    }

    // Test: ExecuteStrategy should return the expected result
    [Fact]
    public void ExecuteStrategy_ShouldReturnExpectedResults()
    {
        // Arrange
        var service = StrategyLoaderService.Service;
        var mockQuotes = new List<Quote> { new Quote() };
        IStrategy strategy;

        _mockStrategy.Setup(i => i.Execute(It.IsAny<IEnumerable<Quote>>(), It.IsAny<object[]>()))
                      .Returns(ActionStrategy.Long);

        service.UpdateStrategy("MyCustomStrategy", _mockStrategy.Object);

        service.TryGetStrategy("MyCustomStrategy", out strategy);

        // Act
        var result = strategy.Execute(mockQuotes);

        // Assert
        Assert.Equal(ActionStrategy.Long, result);
    }

    // Test: RemoveStrategy should remove an existing indicator
    [Fact]
    public void RemoveStrategy_ShouldRemoveStrategy_WhenStrategyExists()
    {
        // Arrange
        var service = StrategyLoaderService.Service;
        var strategyName = "MyCustomStrategy";
        IStrategy strategy;

        service.TryGetStrategy(strategyName, out strategy);

        // Act
        service.RemoveStrategy(strategyName);

        // Assert
        Assert.False(service.TryGetStrategy(strategyName, out var s));
        Assert.Null(s);
    }

    // Test: UpdateStrategy should remove and add the new indicator
    [Fact]
    public void UpdateStrategy_ShouldUpdateStrategy_WhenCalled()
    {
        // Arrange
        var service = StrategyLoaderService.Service;
        var strategyName = "MyCustomStrategy";

        var strategyFileName = $"{strategyName}.cs";

        var filePath = new FileInfo(strategyFileName).FullName;

        if (File.Exists(filePath))
            File.Delete(filePath);

        File.WriteAllText(filePath,
            GetStrategyCode(strategyName)
        );

        // Act
        service.UpdateStrategy(filePath);

        // Assert
        Assert.Contains(strategyName, service.GetCompiledStrategies().Keys);
    }

    // Test: ReloadStrategies should clear and reload all indicators
    [Fact]
    public void ReloadStrategies_ShouldClearAndReloadStrategies()
    {
        // Arrange
        var service = StrategyLoaderService.Service;

        var strategyName = "MyCustomStrategy";

        var strategyFileName = $"{strategyName}.cs";

        var filePath = new FileInfo(strategyFileName).FullName;

        if (File.Exists(filePath))
            File.Delete(filePath);

        File.WriteAllText(filePath, GetStrategyCode(strategyName));

        // Act
        service.ReloadStrategies(_mockStrategyPath);

        // Assert
        Assert.Empty(service.GetCompiledStrategies()); // Should be cleared after reload
    }

    [Fact]
    public void UpdateStrategy_ShouldReturnCorrectData_WhenStrategyExecuted()
    {
        // Arrange
        var service = StrategyLoaderService.Service;
        var strategyName = "MyCustomStrategy";

        var strategyFileName = $"{strategyName}.cs";

        var filePath = new FileInfo(strategyFileName).FullName;

        if (File.Exists(filePath))
            File.Delete(filePath);

        File.WriteAllText(filePath, GetStrategyCode(strategyName));


        // Simulate indicator added
        service.GetCompiledStrategies()[strategyName] = _mockStrategy.Object;

        // Act
        service.UpdateStrategy(filePath);

        // Create some test data for quotes
        var quotes = new List<Quote>
    {
        new Quote { Close = 10m }, // Initialize properties as needed
        new Quote { Close = 20m },
        new Quote { Close = 30m },
    };

        // Call all the strategies wrapper
        var result = service.Evaluate(quotes);

        // Assert
        Assert.Equal(ActionStrategy.Short, result);
    }

    [Fact]
    public void RunRealStrategyWithIndicators()
    {
        // Arrange
        var service = StrategyLoaderService.Service;
        var strategyName = "MyCustomStrategy";

        var strategyFileName = $"{strategyName}.cs";

        var filePath = new FileInfo(strategyFileName).FullName;

        if (File.Exists(filePath))
            File.Delete(filePath);

        var code = GetStrategyCodeWithIndicator(strategyName);

        File.WriteAllText(filePath, code);

        // Simulate indicator added
        service.GetCompiledStrategies()[strategyName] = _mockStrategy.Object;

        // Act
        service.UpdateStrategy(filePath);

        // Create some test data for quotes
        var quotes = new List<Quote>();

        Random random = new Random();
        double lastClose = 100.0;

        for (int i = 0; i < 500; i++)
        {
            double change = (random.NextDouble() - 0.5) * 10; // Changement entre -5 et +5
            lastClose += change;

            quotes.Add(new Quote
            {
                Date = DateTime.Now.AddDays(-i),
                Open = (decimal)(lastClose + (random.NextDouble() * 2 - 1)),  // Variabilité autour de lastClose
                High = (decimal)(lastClose + (random.NextDouble() * 5)),
                Low = (decimal)(lastClose - (random.NextDouble() * 5)),
                Close = (decimal)lastClose,
                Volume = 1000 + (i * 10)  // Augmentation progressive du volume
            });
        }

        // Call all the strategies wrapper
        var result = service.Evaluate(quotes);

        // Assert
        Assert.Equal(ActionStrategy.Long, result);
    }

    [Fact]
    public void RunMyTradingStrategy()
    {
        // Arrange
        var service = StrategyLoaderService.Service;

        // Act
        service.AddStrategy("MyTradingStrategy", new MyTradingStrategy());

        // Create some test data for quotes
        var quotes = new List<Quote>();

        Random random = new Random();
        double lastClose = 100.0;
        
        for (int i = 0; i < 500; i++)
        {
            double change = (random.NextDouble() - 0.5) * 10; // Changement entre -5 et +5
            lastClose += change;

            quotes.Add(new Quote
            {
                Date = DateTime.Now.AddDays(-i),
                Open = (decimal)(lastClose + (random.NextDouble() * 2 - 1)),  // Variabilité autour de lastClose
                High = (decimal)(lastClose + (random.NextDouble() * 5)),
                Low = (decimal)(lastClose - (random.NextDouble() * 5)),
                Close = (decimal)lastClose,
                Volume = 1000 + (i * 10)  // Augmentation progressive du volume
            });
        }

        // Call all the strategies wrapper
        var result = service.Evaluate(quotes);

        // Assert
        Assert.Equal(ActionStrategy.Long, result);
    }
}
