using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public class UnderlyingSystemMethodArgument
    {
        private readonly string _identifier;
        public UnderlyingSystemMethod Method { get; }
        public string Name { get; set; }
        public Type Type { get; set; }

        public string Description { get; set; }


        public UnderlyingSystemMethodArgument(UnderlyingSystemMethod method, string name, Type type)
        {
            Method = method;
            Name = name;
            Type = type;
        }
    }
}
