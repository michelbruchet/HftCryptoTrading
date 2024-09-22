using HftCryptoTrading.Exchanges.BinanceExchange;
using HftCryptoTrading.Exchanges.Core.Exchange;
using HftCryptoTrading.Saga.MarketDownloader.Handlers;
using HftCryptoTrading.Saga.MarketDownloader.Processes;
using HftCryptoTrading.Saga.MarketDownloader.Workers;
using HftCryptoTrading.ServiceDefaults;
using HftCryptoTrading.Shared;
using MediatR;
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

builder.Services.AddSingleton<MarketDownloaderSagaHost>();
builder.Services.AddSingleton<IMarketDownloaderSaga, MarketDownloaderSaga>();
builder.Services.AddHostedService<MarketDownloaderSagaHost>();

builder.Services.AddMediatR(option=>
    {
        option.RegisterServicesFromAssembly(typeof(NewSymbolTickerDataHandler).Assembly);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.UseCors("AllowAllOrigins");

app.Run();