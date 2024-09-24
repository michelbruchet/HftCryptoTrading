using Moq;
using Xunit;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
using HftCryptoTrading.Shared.Strategies;
using Skender.Stock.Indicators;
using HftCryptoTrading.Saga.StrategyEvaluator.Strategies;

namespace HftCryptoTrading.Tests.Strategies;

public class IndicatorLoaderServiceTests
{
    private readonly Mock<IIndicator> _mockIndicator;
    private readonly string _mockIndicatorPath;

    public IndicatorLoaderServiceTests()
    {
        _mockIndicator = new Mock<IIndicator>();
        _mockIndicatorPath = new DirectoryInfo("indicators").FullName;

        if(!Directory.Exists(_mockIndicatorPath))
            Directory.CreateDirectory(_mockIndicatorPath);
    }

    // Test: Check if singleton instance is created
    [Fact]
    public void Service_ShouldReturnSingletonInstance()
    {
        // Act
        var instance1 = IndicatorLoaderService.Service;
        var instance2 = IndicatorLoaderService.Service;

        // Assert
        Assert.Equal(instance1, instance2); // Should be the same instance
    }

    // Test: LoadIndicators throws DirectoryNotFoundException if the path doesn't exist
    [Fact]
    public void LoadIndicators_ShouldThrowDirectoryNotFoundException_WhenPathDoesNotExist()
    {
        // Arrange
        var service = IndicatorLoaderService.Service;
        var nonExistentPath = "nonExistentPath";

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => service.LoadIndicators(nonExistentPath));
    }

    // Test: AddIndicator should throw FileNotFoundException if file doesn't exist
    [Fact]
    public void AddIndicator_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var service = IndicatorLoaderService.Service;
        var nonExistentFile = "nonExistentFile.cs";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => service.AddIndicator(nonExistentFile));
    }

    // Test: ExecuteIndicator should return the expected result
    [Fact]
    public void ExecuteIndicator_ShouldReturnExpectedResults()
    {
        // Arrange
        var service = IndicatorLoaderService.Service;
        var mockQuotes = new List<Quote> { new Quote() };
        var mockResults = new List<decimal> { 1.0m, 2.0m, 3.0m };

        _mockIndicator.Setup(i => i.Execute(It.IsAny<IEnumerable<Quote>>(), It.IsAny<object[]>()))
                      .Returns(mockResults);

        service.GetCompiledIndicators()["TestIndicator"] = _mockIndicator.Object;

        // Act
        var result = service.ExecuteIndicator("TestIndicator", mockQuotes);

        // Assert
        Assert.Equal(mockResults, result);
    }

    // Test: RemoveIndicator should remove an existing indicator
    [Fact]
    public void RemoveIndicator_ShouldRemoveIndicator_WhenIndicatorExists()
    {
        // Arrange
        var service = IndicatorLoaderService.Service;
        var indicatorName = "TestIndicator";
        service.GetCompiledIndicators()[indicatorName] = _mockIndicator.Object;

        // Act
        service.RemoveIndicator(indicatorName);

        // Assert
        Assert.False(service.GetCompiledIndicators().ContainsKey(indicatorName));
    }

    // Test: UpdateIndicator should remove and add the new indicator
    [Fact]
    public void UpdateIndicator_ShouldUpdateIndicator_WhenCalled()
    {
        // Arrange
        var service = IndicatorLoaderService.Service;
        var indicatorFilePath = "MyIndicator.cs";

        var filename = new FileInfo(indicatorFilePath).FullName;
        
        if(File.Exists(filename))
            File.Delete(filename);

        File.WriteAllText(filename,
            @"
        public class MyIndicator : IIndicator
        {
            public IEnumerable<decimal> Execute(IEnumerable<Quote> quotes, params object[] parameters)
            {
                Random random = new System.Random();
                return quotes.Select(q => (decimal)(random.NextDouble() * 100));
            }
        }
");

        var indicatorName = Path.GetFileNameWithoutExtension(indicatorFilePath);

        // Simulate indicator added
        service.GetCompiledIndicators()[indicatorName] = _mockIndicator.Object;

        // Act
        service.UpdateIndicator(indicatorFilePath);

        // Assert
        Assert.Contains(indicatorName, service.GetCompiledIndicators().Keys);
    }

    // Test: ReloadIndicators should clear and reload all indicators
    [Fact]
    public void ReloadIndicators_ShouldClearAndReloadIndicators()
    {
        // Arrange
        var service = IndicatorLoaderService.Service;
        var indicatorName = "TestIndicator";
        service.GetCompiledIndicators()[indicatorName] = _mockIndicator.Object;

        // Act
        service.ReloadIndicators(_mockIndicatorPath);

        // Assert
        Assert.Empty(service.GetCompiledIndicators()); // Should be cleared after reload
    }

    [Fact]
    public void UpdateIndicator_ShouldReturnCorrectData_WhenIndicatorExecuted()
    {
        // Arrange
        var service = IndicatorLoaderService.Service;
        var indicatorFilePath = "MyIndicator.cs";

        var filename = new FileInfo(indicatorFilePath).FullName;

        if (File.Exists(filename))
            File.Delete(filename);

        // Write the indicator code
        File.WriteAllText(filename,
            @"
                public class MyIndicator : IIndicator
                {
                    public IEnumerable<decimal> Execute(IEnumerable<Quote> quotes, params object[] parameters)
                    {
                        return quotes.Select(q => q.Close * 2); // Simple doubling logic
                    }
                }
        ");

        var indicatorName = Path.GetFileNameWithoutExtension(indicatorFilePath);

        // Simulate indicator added
        service.GetCompiledIndicators()[indicatorName] = _mockIndicator.Object;

        // Act
        service.UpdateIndicator(indicatorFilePath);

        // Create some test data for quotes
        var quotes = new List<Quote>
    {
        new Quote { Close = 10m }, // Initialize properties as needed
        new Quote { Close = 20m },
        new Quote { Close = 30m },
    };

        // Call the custom indicator wrapper
        var result = quotes.CustomIndicator(indicatorName, 3, 3, 2, 1);

        // Assert
        Assert.Contains(indicatorName, service.GetCompiledIndicators().Keys);
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Verify the results against expected values
        var expectedResults = new List<decimal> { 20m, 40m, 60m }; // Based on the logic in Execute
        Assert.Equal(expectedResults, result.ToList());
    }
}
