using HftCryptoTrading.Exchanges.BinanceExchange;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.StrategyEvaluator.Handlers;
using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
using HftCryptoTrading.Saga.StrategyEvaluator.Workers;
using HftCryptoTrading.ServiceDefaults;
using HftCryptoTrading.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
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

    ExchangeProviderFactory.RegisterExchange("Binance", loggerFactory, (appSettings, loggerFactory) => new BinanceDownloadMarketClient(appSettings,
        loggerFactory.CreateLogger<BinanceDownloadMarketClient>(), mediatR));

    return new ExchangeProviderFactory(loggerFactory);
});

builder.Services.AddMediatR(option =>
{
    option.RegisterServicesFromAssembly(typeof(ReceiveSymbolAnaylsePriceHandler).Assembly);
});

builder.Services.AddActivatedSingleton(sp =>
{
    StrategyLoaderService.Initialize(sp);
    return StrategyLoaderService.Service;
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

builder.Services.AddSingleton<ExchangeProviderFactory>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var mediatR = sp.GetRequiredService<IMediator>();

    ExchangeProviderFactory.RegisterExchange("Binance", loggerFactory, (appSettings, loggerFactory) => new BinanceDownloadMarketClient(appSettings,
        loggerFactory.CreateLogger<BinanceDownloadMarketClient>(), mediatR));

    return new ExchangeProviderFactory(loggerFactory);
});

builder.Services.AddHostedService<StrategyAnalyserSagaHost>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.UseCors("AllowAllOrigins");

app.Run();