using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chd.OpcUa.Contracts.Interfaces
{
    public interface INotifyItemClient
    {
        event AsyncEventHandler<MonitoredItemEventArgs> MonitoredItemNotification;
        Task<bool> MonitorItem(string node, int sampingInteral = 500, CancellationToken cancellationToken = default);
        bool RemoveMonitorItem(string node);
    }
}
