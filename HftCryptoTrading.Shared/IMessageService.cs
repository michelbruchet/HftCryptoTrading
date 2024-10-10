using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared;

public interface IMessageService
{
    Task<OperationResult> SubscribeAsync(string @namespace, string eventName, string connectionId);
    Task<OperationResult> UnsubscribeAsync(string @namespace, string eventName, string connectionId);
    Task<OperationResult> BroadcastEventAsync(EventMessage message, string connectionId);
}
