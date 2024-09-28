using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var hubApi = builder
    .AddProject<Projects.HftCryptoTrading_ApiService>("hubApi")
    .WithEnvironment("Binance__ApiKey", builder.Configuration["Binance:ApiKey"])
    .WithEnvironment("Binance__ApiSecret", builder.Configuration["Binance:ApiSecret"])
    .WithEnvironment("Binance__IsBackTest", builder.Configuration["Binance:IsBackTest"])
    .WithEnvironment("LimitSymbolsMarket", builder.Configuration["LimitSymbolsMarket"])
    .WithEnvironment("Hub__HubApiKey", builder.Configuration["Hub:HubApiKey"])
    .WithEnvironment("Hub__HubApiSecret", builder.Configuration["Hub:HubApiSecret"])
    .WithEnvironment("Hub__NameSpace", builder.Configuration["Hub:NameSpace"])
    .WithEnvironment("Runtime__IndicatorsPath", builder.Configuration["Runtime:IndicatorsPath"])
    .WithEnvironment("Runtime__StrategiesPath", builder.Configuration["Runtime:StrategiesPath"])
    .WithEnvironment("Trading__Period", builder.Configuration["Trading:Period"])
    .WithEnvironment("Trading__StartElpasedTime", builder.Configuration["Trading:StartElpasedTime"])
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.HftCryptoTrading_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(hubApi)
    .WithEnvironment("Binance__ApiKey", builder.Configuration["Binance:ApiKey"])
    .WithEnvironment("Binance__ApiSecret", builder.Configuration["Binance:ApiSecret"])
    .WithEnvironment("Binance__IsBackTest", builder.Configuration["Binance:IsBackTest"])
    .WithEnvironment("LimitSymbolsMarket", builder.Configuration["LimitSymbolsMarket"])
    .WithEnvironment("Hub__HubApiUrl", builder.Configuration["Hub:HubApiUrl"])
    .WithEnvironment("Hub__HubApiKey", builder.Configuration["Hub:HubApiKey"])
    .WithEnvironment("Hub__HubApiSecret", builder.Configuration["Hub:HubApiSecret"])
    .WithEnvironment("Hub__NameSpace", builder.Configuration["Hub:NameSpace"])
    .WithEnvironment("Runtime__IndicatorsPath", builder.Configuration["Runtime:IndicatorsPath"])
    .WithEnvironment("Runtime__StrategiesPath", builder.Configuration["Runtime:StrategiesPath"])
    .WithEnvironment("Trading__Period", builder.Configuration["Trading:Period"])
    .WithEnvironment("Trading__StartElpasedTime", builder.Configuration["Trading:StartElpasedTime"]);

builder.AddProject<Projects.HftCryptoTrading_Saga_MarketDownloader>("hftcryptotrading-saga-marketdownloader")
    .WithReference(cache)
    .WithReference(hubApi)
    .WithEnvironment("Binance__ApiKey", builder.Configuration["Binance:ApiKey"])
    .WithEnvironment("Binance__ApiSecret", builder.Configuration["Binance:ApiSecret"])
    .WithEnvironment("Binance__IsBackTest", builder.Configuration["Binance:IsBackTest"])
    .WithEnvironment("LimitSymbolsMarket", builder.Configuration["LimitSymbolsMarket"])
    .WithEnvironment("Hub__HubApiKey", builder.Configuration["Hub:HubApiKey"])
    .WithEnvironment("Hub__HubApiSecret", builder.Configuration["Hub:HubApiSecret"])
    .WithEnvironment("Hub__HubApiUrl", builder.Configuration["Hub:HubApiUrl"])
    .WithEnvironment("Hub__NameSpace", builder.Configuration["Hub:NameSpace"])
    .WithEnvironment("Runtime__IndicatorsPath", builder.Configuration["Runtime:IndicatorsPath"])
    .WithEnvironment("Runtime__StrategiesPath", builder.Configuration["Runtime:StrategiesPath"])
    .WithEnvironment("Trading__Period", builder.Configuration["Trading:Period"])
    .WithEnvironment("Trading__StartElpasedTime", builder.Configuration["Trading:StartElpasedTime"]);

builder.AddProject<Projects.HftCryptoTrading_Saga_StrategyEvaluator>("hftcryptotrading-saga-strategyevaluator")
    .WithReference(cache)
    .WithReference(hubApi)
    .WithEnvironment("Binance__ApiKey", builder.Configuration["Binance:ApiKey"])
    .WithEnvironment("Binance__ApiSecret", builder.Configuration["Binance:ApiSecret"])
    .WithEnvironment("Binance__IsBackTest", builder.Configuration["Binance:IsBackTest"])
    .WithEnvironment("LimitSymbolsMarket", builder.Configuration["LimitSymbolsMarket"])
    .WithEnvironment("Hub__HubApiKey", builder.Configuration["Hub:HubApiKey"])
    .WithEnvironment("Hub__HubApiSecret", builder.Configuration["Hub:HubApiSecret"])
    .WithEnvironment("Hub__HubApiUrl", builder.Configuration["Hub:HubApiUrl"])
    .WithEnvironment("Hub__NameSpace", builder.Configuration["Hub:NameSpace"])
    .WithEnvironment("Runtime__IndicatorsPath", builder.Configuration["Runtime:IndicatorsPath"])
    .WithEnvironment("Runtime__StrategiesPath", builder.Configuration["Runtime:StrategiesPath"])
    .WithEnvironment("Trading__Period", builder.Configuration["Trading:Period"])
    .WithEnvironment("Trading__StartElpasedTime", builder.Configuration["Trading:StartElpasedTime"]);

builder.Build().Run();