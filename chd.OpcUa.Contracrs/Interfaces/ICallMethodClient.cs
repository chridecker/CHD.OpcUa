using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chd.OpcUa.Contracts.Interfaces
{
    public interface ICallMethodClient
    {
        Task<IEnumerable<object>> CallMethod(string method, CancellationToken cancellationToken = default, params object[] inputs);

        Task<TOutput> CallMethod<TInput, TOutput>(string method, TInput input,
            CancellationToken cancellationToken = default)
            where TInput : struct
            where TOutput : struct;
    }
}
