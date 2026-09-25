using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Contracts.Interfaces;
using chd.OpcUa.Server.ObjectSystem;
using chd.OpcUa.ServerWorker.UaServerObjects;

namespace chd.OpcUa.ServerWorker
{
    public class chdObjectSystemManager : ObjectSystemManager
    {
        protected override List<IUaServerObject> GetObjects()
        {
            var lst = new List<IUaServerObject>()
            {
                new Calculator("CHDCalc"),
                new UaTimer("CHDTimer1"),
                new UaTimer("CHDTimer2"),
            };
            return lst;
        }
    }
}
