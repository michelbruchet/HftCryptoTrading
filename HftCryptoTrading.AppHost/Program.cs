var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.HftCryptoTrading_ApiService>("apiservice");

builder.AddProject<Projects.HftCryptoTrading_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService);


builder.AddProject<Projects.HftCryptoTrading_Saga_MarketDownloader>("hftcryptotrading-saga-marketdownloader");


builder.Build().Run();
