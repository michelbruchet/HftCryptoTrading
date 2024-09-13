using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace HftCryptoTrading.ApiServices.E2ETests
{
    [Binding]
    public sealed class AspireHostHook
    {
        public DistributedApplication App { get; set; }

        [BeforeScenario]
        public async Task BeforeScenario()
        {
            // Arrange
            var appHost = await DistributedApplicationTestingBuilder
                .CreateAsync<Projects.HftCryptoTrading_AppHost>();

            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            App = await appHost.BuildAsync();
            await App.StartAsync();
        }

        [AfterScenario]
        public async Task AfterScenario()
        {
            await App.DisposeAsync();
        }
    }
}