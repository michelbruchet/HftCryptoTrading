
using HftCryptoTrading.Customers.Shared;
using HftCryptoTrading.ServiceDefaults;

namespace HftCryptoTrading.Customers;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        var customers = new Customer[]
        {
            new Customer
            {
                Name = "Test",
                ApiKey = "YtMBhnEOKjv8b0Ql0RhKTpuZEptLqHCvB0PZPPgTnMYmacOtmQ5LPSDjrRINp3XL",
                ApiSecret = "YxgBiRjfc2Bzi44fZh8FftDEFEA1phM2KuKprJ1RU6cY23lauCmsMR1w6MtkBfpf",
                ApiToken = "token",
                Email = "hftcryptotrading001@mailinator.com",
                ExchangeName = "Binance",
                IsBackTest = true,
            }
        };

        app.MapGet("/customers", (HttpContext httpContext) =>
        {
            return customers;
        })
        .WithName("GetCustomers")
        .WithOpenApi();

        app.Run();
    }
}
