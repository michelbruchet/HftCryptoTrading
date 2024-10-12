using HftCryptoTrading.Client;
using HftCryptoTrading.Exchanges.BinanceExchange;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.OpenPositionMonitor;
using HftCryptoTrading.Saga.OpenPositionMonitor.Handlers;
using HftCryptoTrading.Saga.StrategyEvaluator.Indicators;
using HftCryptoTrading.ServiceDefaults;
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

builder.Services.AddSingleton<IExchangeProviderFactory>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var mediatR = sp.GetRequiredService<IMediator>();
    var distributedCache = sp.GetRequiredService<IDistributedCache>();

    ExchangeProviderFactory.RegisterExchange("Binance", loggerFactory, (appSettings, loggerFactory) =>
    new BinanceDownloadMarketClient(appSettings,
        loggerFactory.CreateLogger<BinanceDownloadMarketClient>(), mediatR, distributedCache));

    return new ExchangeProviderFactory(loggerFactory);
});

builder.Services.AddHttpClient("ApiCustomer",
    static client => client.BaseAddress = new("https+http://hftcryptotrading-customers"));

builder.Services.AddMediatR(option => {
    option.RegisterServicesFromAssembly(typeof(DownloadOpenOrdersRequestHandler).Assembly);
});

builder.Services.AddSingleton<IOpenPositionMonitorSaga, OpenPositionMonitorSaga>();

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

builder.Services.AddHostedService<OpenPositionMonitorBackgrounService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.UseCors("AllowAllOrigins");

app.Run();