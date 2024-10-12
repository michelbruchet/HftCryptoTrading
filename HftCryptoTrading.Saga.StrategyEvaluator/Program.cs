using HftCryptoTrading.Client;
using HftCryptoTrading.Exchanges.BinanceExchange;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.StrategyEvaluator.Handlers;
using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
using HftCryptoTrading.Saga.StrategyEvaluator.Workers;
using HftCryptoTrading.ServiceDefaults;
using HftCryptoTrading.Services.Commands;
using HftCryptoTrading.Shared.Metrics;
using HftCryptoTrading.Shared.Saga;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StrategyExecution;

AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
{
    Console.WriteLine(e.ToString());
};

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyMethod()
                   .AllowAnyHeader()
                   .SetIsOriginAllowed(origin => true)
                   .AllowCredentials();
        });
});

// Bind AppSettings to the configuration section in appsettings.json
builder.Services.Configure<AppSettings>(builder.Configuration);

builder.Services.AddSingleton<ExchangeProviderFactory>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var mediatR = sp.GetRequiredService<IMediator>();
    var distributedCache = sp.GetRequiredService<IDistributedCache>();

    ExchangeProviderFactory.RegisterExchange("Binance", loggerFactory, (appSettings, loggerFactory) => 
    new BinanceDownloadMarketClient(appSettings,
        loggerFactory.CreateLogger<BinanceDownloadMarketClient>(), mediatR, distributedCache));

    return new ExchangeProviderFactory(loggerFactory);
});

builder.Services.AddMediatR(option =>
{
    option.RegisterServicesFromAssembly(typeof(DownloadSymbolHistoryEventHandler).Assembly);
});

builder.Services.AddActivatedSingleton(sp =>
{
    var strategyLoaderService = new StrategyLoaderService(sp.GetRequiredService<IMetricService>());
    var appsettings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
    strategyLoaderService.LoadStrategies(appsettings.Runtime.StrategiesPath);
    return strategyLoaderService;
});

builder.Services.AddActivatedSingleton(sp =>
{
    IndicatorLoaderService.Initialize(sp);
    return IndicatorLoaderService.Service;
});

builder.Services.AddSingleton<IStrategyAnalyserSaga, StrategyAnalyserSaga>();

builder.AddRedisDistributedCache("cache", configureOptions: options => options.ConnectTimeout = 3000);

// Bind AppSettings to the configuration section in appsettings.json
builder.Services.Configure<AppSettings>(builder.Configuration);

builder.Services.AddSingleton<IHubClientPublisherFactory, HubClientPublisherFactory>();

builder.Services.AddSingleton<ExchangeProviderFactory>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var mediatR = sp.GetRequiredService<IMediator>();
    var distributedCache = sp.GetRequiredService<IDistributedCache>();

    ExchangeProviderFactory.RegisterExchange("Binance", loggerFactory, (appSettings, loggerFactory) => 
    new BinanceDownloadMarketClient(appSettings,
        loggerFactory.CreateLogger<BinanceDownloadMarketClient>(), mediatR, distributedCache));

    return new ExchangeProviderFactory(loggerFactory);
});

builder.Services.AddHostedService<StrategyAnalyserSagaHost>();


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.UseCors("AllowAllOrigins");

app.Run();