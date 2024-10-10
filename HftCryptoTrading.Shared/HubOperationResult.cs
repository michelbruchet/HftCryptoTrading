namespace HftCryptoTrading.Shared;

public record class OperationResult(bool IsSuccess, string? ErrorMessage = null);

public record class SuccessOperationResult() : OperationResult(true);

public record class FailedOperationResult(string ErrorMessage) : OperationResult(false, ErrorMessage);
