using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.ObjectSystem
{
    public class ObjectSystemPropertyAttribute : ObjectSystemAttribute
    {
        public ObjectSystemPropertyAttribute(string displayName = null) : base(displayName)
        {
        }
    }
}
