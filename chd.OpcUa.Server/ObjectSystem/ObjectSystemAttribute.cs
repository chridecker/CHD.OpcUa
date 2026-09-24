using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.ObjectSystem
{
    public abstract class ObjectSystemAttribute : Attribute
    {
        public string DisplayName { get;}
        public bool CanWrite { get; set; }
        protected ObjectSystemAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
