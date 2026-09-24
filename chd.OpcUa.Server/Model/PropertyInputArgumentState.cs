using chd.OpcUa.Server.UnderlyingSystem;
using chd.OpcUa.ServerWorker;
using Opc.Ua;
using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.Text;
using LocalizedText = Opc.Ua.LocalizedText;

namespace chd.OpcUa.Server.Model
{
    public class PropertyInputArgumentState : PropertyArgumentState
    {

        public PropertyInputArgumentState(UnderlyingSystemMethod method, NodeId nodeId, NodeState? parent) 
            : base(method, nodeId, BrowseNames.InputArguments, x => x.InputArguments, parent)
        {
        }
    }
}
