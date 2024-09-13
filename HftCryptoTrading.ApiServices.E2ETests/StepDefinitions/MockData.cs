using MessagePack;

namespace HftCryptoTrading.ApiServices.E2ETests.StepDefinitions;

[MessagePackObject]
public class MockData(Guid id, DateTime date)
{
    [Key(0)]
    public Guid Id { get; set; } = id;

    [Key(1)]
    public DateTime Date { get; set; } = date;
    
}