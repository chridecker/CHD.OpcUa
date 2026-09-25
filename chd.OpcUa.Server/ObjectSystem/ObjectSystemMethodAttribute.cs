using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.ObjectSystem
{
    public class ObjectSystemMethodAttribute(string displayName = null) : ObjectSystemAttribute(displayName)
    {
        public bool CanExecute { get; set; } = true;
    }
}
