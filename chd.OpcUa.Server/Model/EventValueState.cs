using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace chd.OpcUa.Server.Model
{
    public class EventValueState : PropertyState<Variant>
    {
        public EventValueState(NodeState? parent) : base(parent)
        {
        }

        public override Variant Value { get; set; }
    }
}
