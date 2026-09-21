using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace chd.OpcUa.Contracts
{
    public delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e)
        where TEventArgs : EventArgs;
}
