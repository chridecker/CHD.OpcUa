using chd.OpcUa.Server.Interfaces;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.ServerWorker
{
    public class NodeManagerFactory(INamespaceManager namespaceManager, IUnderlyingSystemManager underlyingSystemManager) : IAsyncNodeManagerFactory
    {
        public ValueTask<IAsyncNodeManager> CreateAsync(IServerInternal server, ApplicationConfiguration configuration,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return new ValueTask<IAsyncNodeManager>(new NodeManager(server, configuration, underlyingSystemManager, NamespacesUris.ToArray()));
        }

        public ArrayOf<string> NamespacesUris => namespaceManager.NameSpaces;
    }
}
