using chd.OpcUa.Server.Interfaces;
using chd.OpcUa.Server.ObjectSystem;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.ServerWorker.UaServerObjects
{
    public class UaTimer : BaseUaServerObject
    {
        public UaTimer(string name) : base(name)
        {
        }
    }
}
