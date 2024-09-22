var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var hubApi = builder.AddProject<Projects.HftCryptoTrading_ApiService>("apiservice");

builder.AddProject<Projects.HftCryptoTrading_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(hubApi);

builder.AddProject<Projects.HftCryptoTrading_Saga_MarketDownloader>("hftcryptotrading-saga-marketdownloader")
    .WithReference(cache)
    .WithReference(hubApi);

builder.Build().Run();