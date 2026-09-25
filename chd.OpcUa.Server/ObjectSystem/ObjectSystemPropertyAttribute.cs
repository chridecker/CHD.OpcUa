using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.ObjectSystem
{
    public class ObjectSystemPropertyAttribute(string displayName = null) : ObjectSystemAttribute(displayName)
    {
        public bool CanWrite { get; set; }
    }
}
