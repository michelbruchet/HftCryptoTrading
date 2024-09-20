using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HftCryptoTrading.Shared.Metrics;

public interface IMetricService
{
    IDisposable StartTracking(string operationName);
    void TrackSuccess(string operationName);
    void TrackFailure(string operationName, Exception exception);
    void TrackFailure(string operationName);
}
