using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.ObjectSystem
{
    public class ObjectSystemMethodAttribute : ObjectSystemAttribute
    {
        public bool CanExecute { get; set; } = true;

        public ObjectSystemMethodAttribute(string displayName = null) : base(displayName)
        {
        }
    }
}
