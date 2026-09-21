using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chd.OpcUa.Contracts.Interfaces
{
    public interface IWriteClient
    {
        Task<bool> WriteAsync<T>(string node, T value, CancellationToken cancellationToken);
    }
}
