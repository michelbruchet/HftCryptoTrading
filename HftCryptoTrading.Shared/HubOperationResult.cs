using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared;

public record class OperationResult(bool IsSuccess, string? ErrorMessage = null);

public record class SuccessOperationResult() : OperationResult(true);

public record class FailedOperationResult(string ErrorMessage) : OperationResult(false, ErrorMessage);
