using chd.OpcUa.Server.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.ServerWorker
{
    public class NamespaceManager : INamespaceManager
    {
        public string[] NameSpaces => [Namespaces.DataAccess];
    }
}
