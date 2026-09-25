using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.ObjectSystem
{
    public class ObjectSystemPropertyAttribute : ObjectSystemAttribute
    {
        public bool CanWrite { get; set; }

        public ObjectSystemPropertyAttribute(string displayName = null) : base(displayName)
        {
        }
    }
}
