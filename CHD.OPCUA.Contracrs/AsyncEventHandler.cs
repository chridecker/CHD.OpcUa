using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chd.OpcUa.Contracts
{
    public delegate ValueTask AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e, CancellationToken cancellationToken = default)
        where TEventArgs : class;
}
