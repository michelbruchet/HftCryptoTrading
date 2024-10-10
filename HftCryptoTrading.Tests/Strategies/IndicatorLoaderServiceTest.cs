using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
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

public class IndicatorLoaderServiceTest
{
    private Mock<IMetricService> _metricServiceMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private IndicatorLoaderService _indicatorLoaderService;

    public IndicatorLoaderServiceTest()
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

        // Initialize the IndicatorLoaderService with the mocked service provider
        IndicatorLoaderService.Initialize(_serviceProviderMock.Object);
        _indicatorLoaderService = IndicatorLoaderService.Service;

    }

    [Fact]
    public void LoadIndicators_InvalidPath_ShouldTrackFailure()
    {
        // Arrange
        var invalidPathService = IndicatorLoaderService.Service;

        // Act
        invalidPathService.LoadIndicators("invalidPath");

        // Assert
        _metricServiceMock.Verify(
            m => m.TrackFailure(It.Is<string>(msg => msg.Contains("does not exist"))),
            Times.Once
        );
    }

    [Fact]
    public void CompileIndicator_InvalidFile_ShouldTrackFailure()
    {
        var dirPath = new DirectoryInfo("indicators").FullName;

        if (Directory.Exists(dirPath))
            Directory.Delete(dirPath, true);

        Directory.CreateDirectory(dirPath);

        var testIndicatorsPath = new FileInfo(Path.Combine(dirPath, "MyIndicator.cs")).FullName;

        // Arrange
        File.WriteAllText(testIndicatorsPath, @"InvalidContentFile"); // Un fichier incorrect

        // Act
        Assert.Throws<InvalidOperationException>(() => _indicatorLoaderService.LoadIndicators(dirPath));

        // Assert : vérifie que TrackFailure a été appelé
        _metricServiceMock.Verify(
            m => m.TrackFailure(It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public void LoadIndicators_ValidFiles_ShouldTrackSuccess()
    {

        var dirPath = new DirectoryInfo("indicators").FullName;

        if (Directory.Exists(dirPath))
            Directory.Delete(dirPath, true);

        Directory.CreateDirectory(dirPath);

        var testIndicatorsPath = Path.Combine(dirPath, "MyCustomIndicator.cs");

        // Arrange
        var validCode = @"
            public class MyCustomIndicator : IIndicator
            {
                public IEnumerable<decimal> Execute(IEnumerable<Quote> quotes, params object[] parameters)
                {
                    Random random = new System.Random();
                    return quotes.Select(q => (decimal)(random.NextDouble() * 100));
                }
            }";

        File.WriteAllText(testIndicatorsPath, validCode);

        // Act
        _indicatorLoaderService.LoadIndicators(dirPath);

        // Assert
        _metricServiceMock.Verify(m => m.TrackSuccess("CompileIndicator"), Times.Once);
    }

    [Fact]
    public void ExecuteIndicator_ExistingIndicator_ShouldReturnResult()
    {
        // Arrange
        var dirPath = new DirectoryInfo("indicators").FullName;

        if (Directory.Exists(dirPath))
            Directory.Delete(dirPath, true);

        Directory.CreateDirectory(dirPath);

        var testIndicatorsPath = Path.Combine(dirPath, "MyCustomIndicator.cs");

        if (File.Exists(testIndicatorsPath))
            File.Delete(testIndicatorsPath);

        // Arrange
        var validCode = @"
            public class MyCustomIndicator : IIndicator
            {
                public IEnumerable<decimal> Execute(IEnumerable<Quote> quotes, params object[] parameters)
                {
                    Random random = new System.Random();
                    return quotes.Select(q => (decimal)(random.NextDouble() * 100));
                }
            }";

        File.WriteAllText(testIndicatorsPath, validCode);

        // Act
        _indicatorLoaderService.LoadIndicators(dirPath);
        var quotes = new List<Quote> { new Quote { Close = 100 }, new Quote { Close = 200 } };

        // Act
        var result = _indicatorLoaderService.ExecuteIndicator("MyCustomIndicator", quotes);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void ExecuteIndicator_NonExistentIndicator_ShouldThrowKeyNotFoundException()
    {
        var dirPath = new DirectoryInfo("indicators").FullName;

        if (Directory.Exists(dirPath))
            Directory.Delete(dirPath, true);

        Directory.CreateDirectory(dirPath);

        // Arrange
        var quotes = new List<Quote> { new Quote { Close = 100 } };

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => _indicatorLoaderService.ExecuteIndicator("NonExistentIndicator", quotes));
    }
}