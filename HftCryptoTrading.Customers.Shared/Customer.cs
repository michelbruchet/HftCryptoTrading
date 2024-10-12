namespace HftCryptoTrading.Customers.Shared;

public class Customer
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string ExchangeName { get; set; }
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
    public string ApiToken { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool IsBackTest { get; set; }
}
