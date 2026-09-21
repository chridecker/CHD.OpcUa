using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CHD.OPCUA.Contracts.Interfaces
{
    public interface IReadClient 
    {
        Task<T> ReadAsync<T>(string node,CancellationToken cancellationToken);
    }
}
