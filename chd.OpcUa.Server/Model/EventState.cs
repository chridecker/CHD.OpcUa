using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace chd.OpcUa.Server.Model
{
    public class EventState : BaseEventState
    {
        public EventState(NodeState? parent) : base(parent)
        {
        }
    }
}
