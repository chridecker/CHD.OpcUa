using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Contracts
{
    public class EventTriggeredEventArgs : EventArgs
    {
        public string Message { get; }

        public EventTriggeredEventArgs(string message)
        {
            Message = message;
        }
    }
}
