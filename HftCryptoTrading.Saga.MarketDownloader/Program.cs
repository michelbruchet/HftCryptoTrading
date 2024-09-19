using HftCryptoTrading.Exchanges.BinanceExchange;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.MarketDownloader.Workers;
using HftCryptoTrading.ServiceDefaults;
using HftCryptoTrading.Shared;
using Microsoft.Extensions.DependencyInjection;

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

builder.Services.AddSingleton<AppSettings>();

builder.Services.AddSingleton<ExchangeProviderFactory>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    ExchangeProviderFactory.RegisterExchange("Binance", loggerFactory, (appSettings, loggerFactory) => new BinanceDownloadMarketClient(appSettings,
        loggerFactory.CreateLogger<BinanceDownloadMarketClient>()));

    var factory = new ExchangeProviderFactory(loggerFactory);
    var appSettings = new AppSettings();

    return factory;
});

builder.Services.AddSingleton<MarketDownloaderSagaHost>();
builder.Services.AddSingleton<MarketDownloaderSaga>();
builder.Services.AddHostedService<MarketDownloaderSagaHost>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.UseCors("AllowAllOrigins");

app.Run();