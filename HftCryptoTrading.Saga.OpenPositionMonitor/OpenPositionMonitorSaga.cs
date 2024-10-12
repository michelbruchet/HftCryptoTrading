using HftCryptoTrading.Customers.Shared;
using HftCryptoTrading.Services.Commands;
using HftCryptoTrading.Shared.Saga;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace HftCryptoTrading.Saga.OpenPositionMonitor;

public class OpenPositionMonitorSaga(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider): IOpenPositionMonitorSaga
{
    static ConcurrentDictionary<string, IOpenPositionMonitorSagaContainer> _containers = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await LoadCustomers(httpClientFactory, serviceProvider, cancellationToken);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(15));
            await LoadCustomers(httpClientFactory, serviceProvider, cancellationToken);
        }
    }

    private static async Task LoadCustomers(IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient("ApiCustomer");

        var customers = await httpClient.GetFromJsonAsync<List<Customer>>("/customers", cancellationToken);

        foreach (var customer in customers)
        {
            if (_containers.ContainsKey(customer.Id))
                continue;

            var container = new OpenPositionMonitorSagaContainer(serviceProvider, customer);

            _containers.TryAdd(customer.Id, container);
            await container.StartAsync(cancellationToken);
        }
    }
}
