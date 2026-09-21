using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CHD.OPCUA.Contracts
{
    public class MonitoredItemEventArgs : EventArgs
    {
        public DateTime Time { get; set; }
        public object Value { get; set; }
        public string Node { get; set; }

        public MonitoredItemEventArgs(string node, object value, DateTime time)
        {
            Time = time;
            Value = value;
            Node = node;
        }
    }

    
}
