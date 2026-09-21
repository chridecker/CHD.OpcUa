using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CHD.OPCUA.Contracts.Interfaces
{
    public interface IOpcUAClient : IReadClient, IWriteClient
    {
       event EventHandler<MonitoredItemEventArgs> MonitoredItemNotification;
       Task StartAsync(CancellationToken cancellationToken);
       Task StopAsync(CancellationToken cancellationToken);
       bool IsConnected { get; }


    }
}
