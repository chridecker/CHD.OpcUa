using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CHD.OPCUA.Contracts.Interfaces
{
    public interface INotifiyItemClient
    {
        event AsyncEventHandler<MonitoredItemEventArgs> MonitoredItemNotification;
        Task<bool> MonitorItem(string node, int sampingInteral = 500, CancellationToken cancellationToken = default);
    }
}
