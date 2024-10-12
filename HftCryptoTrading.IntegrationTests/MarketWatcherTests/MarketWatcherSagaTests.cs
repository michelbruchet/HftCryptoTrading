using HftCryptoTrading.Client;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Services.Commands;
using HftCryptoTrading.Shared.Events;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Models;
using HftCryptoTrading.Shared.Saga;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.IntegrationTests.MarketWatcherTests
{
    public class MarketWatcherSagaTests
    {
        public Random rdm = new Random();

        [Fact]
        public async Task WhenStartSagaWithAnExchangeItShouldCallDownloadSymbolCommand()
        {
            //Arrange
            AppSettings appSettings = new AppSettings();
            var mediator = new Mock<IMediator>();
            var loggerFactory = new LoggerFactory();
            var factory = new Mock<IExchangeProviderFactory>();
            var metricService = new Mock<IMetricService>();
            var testExchangeClient = new Mock<IExchangeClient>();
            var hubClientPublisherFactory = new Mock<IHubClientPublisherFactory>();
            var hubClientPublisher = new Mock<IHubClientPublisher>();

            appSettings.LimitSymbolsMarket = 50;

            appSettings.Hub = new HubSetting
            {
                HubApiKey = "skdfmlsfkms",
                HubApiSecret = "skdmflskfmlsd",
                HubApiUrl = "http://localhost",
                NameSpace = "TestGroup"
            };

            testExchangeClient.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(new List<SymbolData>
            {
                new SymbolData("TESTBNB")
            });

            testExchangeClient.Setup(e => e.GetCurrentTickersAsync()).ReturnsAsync(new List<TickerData>
            {
                new TickerData("TESTBNB", "TestExchange")
                {
                    ChangePercentage = 2,
                    Volume = (decimal)rdm.NextDouble()
                }
            });

            factory.Setup(f => f.GetAllExchanges()).Returns(new[] { "TestExchange" });
            factory.Setup(f => f.GetExchange("TestExchange", appSettings, loggerFactory)).Returns(testExchangeClient.Object);

            hubClientPublisherFactory.Setup(f => f.Initialize(appSettings, It.IsAny<string>())).Returns(hubClientPublisher.Object);

            // Act
            var saga = new MarketWatcherSaga(Options.Create<AppSettings>(appSettings), mediator.Object, loggerFactory,
                factory.Object, metricService.Object,
                hubClientPublisherFactory.Object);

            await saga.StartAsync(CancellationToken.None);

            // Verify
            metricService.Verify(m => m.StartTracking("publish-symbols"), Times.Once);
            metricService.Verify(m => m.TrackSuccess("publish-symbols"), Times.Once);

            hubClientPublisher.Verify(p => p.StartAsync(appSettings.Hub.NameSpace), Times.Once);

            mediator.Verify(m => m.Send(It.IsAny<PublishedDownloadedSymbolsEvent>(), It.IsAny<CancellationToken>()), Times.Once);

            //Stop saga
            await saga.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task WhenDownloadSymbolAnalysedSuccessFully()
        {
            //Arrange
            AppSettings appSettings = new AppSettings();
            var mediator = new Mock<IMediator>();
            var loggerFactory = new LoggerFactory();
            var factory = new Mock<IExchangeProviderFactory>();
            var metricService = new Mock<IMetricService>();
            var testExchangeClient = new Mock<IExchangeClient>();
            var hubClientPublisherFactory = new Mock<IHubClientPublisherFactory>();
            var hubClientPublisher = new Mock<IHubClientPublisher>();
            var mockCacheService = new Mock<IDistributedCache>();
            var symbolAnalysisHelper = new Mock<ISymbolAnalysisHelper>();

            appSettings.LimitSymbolsMarket = 50;

            appSettings.Hub = new HubSetting
            {
                HubApiKey = "skdfmlsfkms",
                HubApiSecret = "skdmflskfmlsd",
                HubApiUrl = "http://localhost",
                NameSpace = "TestGroup"
            };

            var services = new ServiceCollection();

            services.AddSingleton(appSettings);
            services.AddSingleton(mediator.Object);
            services.AddSingleton<ILoggerFactory>(loggerFactory);
            services.AddSingleton(factory.Object);
            services.AddSingleton(metricService.Object);
            services.AddSingleton(hubClientPublisherFactory.Object);
            services.AddSingleton(mockCacheService.Object);
            services.AddSingleton(symbolAnalysisHelper.Object);
            
            testExchangeClient.Setup(e => e.GetSymbolsAsync()).ReturnsAsync(new List<SymbolData>
            {
                new SymbolData("TESTBNB")
            });

            testExchangeClient.Setup(e => e.GetCurrentTickersAsync()).ReturnsAsync(new List<TickerData>
            {
                new TickerData("TESTBNB", "TestExchange")
                {
                    ChangePercentage = 2,
                    Volume = (decimal)rdm.NextDouble()
                }
            });

            factory.Setup(f => f.GetAllExchanges()).Returns(new[] { "TestExchange" });
            factory.Setup(f => f.GetExchange("TestExchange", appSettings, loggerFactory)).Returns(testExchangeClient.Object);

            hubClientPublisherFactory.Setup(f => f.Initialize(appSettings, It.IsAny<string>())).Returns(hubClientPublisher.Object);

            var saga = new MarketWatcherSaga(Options.Create<AppSettings>(appSettings), mediator.Object, loggerFactory,
                factory.Object, metricService.Object,
                hubClientPublisherFactory.Object);

            services.AddSingleton<IMarketWatcherSaga>(saga);

            var serviceProvider = services.BuildServiceProvider();

            var PublishedDownloadedSymbolsHandler =
                new HftCryptoTrading.Saga.MarketWatcher.Handlers.PublishedDownloadedSymbolsHandler(serviceProvider,
                Options.Create<AppSettings>(appSettings), loggerFactory.CreateLogger<HftCryptoTrading.Saga.MarketWatcher.Handlers.PublishedDownloadedSymbolsHandler>());

            mediator.Setup(m => m.Send(It.IsAny<PublishedDownloadedSymbolsEvent>(), It.IsAny<CancellationToken>()))
                .Callback(async (PublishedDownloadedSymbolsEvent @event, CancellationToken token) =>
                {
                    await PublishedDownloadedSymbolsHandler.Handle(@event, token);
                });

            symbolAnalysisHelper.Setup(s => s.IsAbnormalPrice(It.IsAny<SymbolTickerData>())).ReturnsAsync(false);
            symbolAnalysisHelper.Setup(s => s.IsAbnormalSpreadBidAsk(It.IsAny<SymbolTickerData>())).ReturnsAsync(false);
            symbolAnalysisHelper.Setup(s => s.IsAbnormalVolume(It.IsAny<SymbolTickerData>())).ReturnsAsync(false);

            // Act

            await saga.StartAsync(CancellationToken.None);

            // Verify
            metricService.Verify(m => m.StartTracking("publish-symbols"), Times.Once);
            metricService.Verify(m => m.TrackSuccess("publish-symbols"), Times.Once);

            hubClientPublisher.Verify(p => p.StartAsync(appSettings.Hub.NameSpace), Times.Once);

            mediator.Verify(m => m.Send(It.IsAny<PublishedDownloadedSymbolsEvent>(), It.IsAny<CancellationToken>()), Times.Once);

            //Stop saga
            await saga.StopAsync(CancellationToken.None);
        }
    }
}
