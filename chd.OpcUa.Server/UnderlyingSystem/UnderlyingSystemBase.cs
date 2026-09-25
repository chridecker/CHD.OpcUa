using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.UnderlyingSystem
{
    public abstract class UnderlyingSystemBase
    {
        public string Name { get; }
        public string Description { get; set; }
        public virtual string Identifier => Name;

        protected UnderlyingSystemBase(string name)
        {
            Name = name;
        }
    }
}
