using Opc.Ua.Server;
using Opc.Ua.Server.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Server.Interfaces;

namespace chd.OpcUa.Server
{
    public class UaServerStartup(IUnderlyingSystemManager systemManager) : IServerStartupTask
    {
        public ValueTask OnServerStartedAsync(IServerContext server, CancellationToken cancellationToken = new CancellationToken())
        {
            return systemManager.InitializeAsync(cancellationToken);
        }
    }
}
