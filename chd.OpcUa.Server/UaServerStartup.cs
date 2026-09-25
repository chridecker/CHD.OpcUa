using chd.OpcUa.Contracts.Interfaces;
using chd.OpcUa.Server.UnderlyingSystem;
using Opc.Ua.Server;
using Opc.Ua.Server.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server
{
    public class UaServerStartup(IUnderlyingSystemManager<UnderlyingSystemSegment, UnderlyingSystemBlock, UnderlyingSystemMethod> systemManager) : IServerStartupTask
    {
        public ValueTask OnServerStartedAsync(IServerContext server, CancellationToken cancellationToken = new CancellationToken())
        {
            return systemManager.InitializeAsync(cancellationToken);
        }
    }
}
