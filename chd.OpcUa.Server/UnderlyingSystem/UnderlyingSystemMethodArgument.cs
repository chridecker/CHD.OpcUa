using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemMethodArgument : UnderlyingSystemBase
    {
        public UnderlyingSystemMethod Method { get; }
        public Type Type { get; set; }

        public UnderlyingSystemMethodArgument(UnderlyingSystemMethod method, string name, Type type) : base(name)
        {
            Method = method;
            Type = type;
        }
    }
}
