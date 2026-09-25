using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.Interfaces
{
    public interface IUaServerObject
    {
        public string Name { get; }
        public string Description { get; }
    }
}
