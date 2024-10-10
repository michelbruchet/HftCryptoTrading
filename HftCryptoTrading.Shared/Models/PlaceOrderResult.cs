namespace HftCryptoTrading.Shared.Models;

public class PlaceOrderResult(bool success, OpenOrder openPosition, string? errorMessage)
{
    public bool Success => success;
    public OpenOrder OpenPosition => openPosition;
    public string? ErrorMessage { get; set; } = errorMessage;
}
