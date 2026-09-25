using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace chd.OpcUa.Server.Model
{
    public class EventState : BaseEventState
    {
        public EventValueState? Value { get; set; }
        public EventState(NodeState? parent) : base(parent)
        {
            Value = new(this);
        }
        //protected override NodeId GetDefaultTypeDefinitionId(
        //    NamespaceTable namespaceUris)
        //{
        //    ushort index = namespaceUris.GetIndexOrAppend(MyNamespaces.Uri);

        //    return new NodeId(MyNodeIds.MyEventType, index);
        //}
    }
}
