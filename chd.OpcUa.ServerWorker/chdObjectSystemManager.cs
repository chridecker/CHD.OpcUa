using System;
using System.Collections.Generic;
using System.Text;
using chd.OpcUa.Server;
using chd.OpcUa.Server.Interfaces;
using chd.OpcUa.ServerWorker.UaServerObjects;

namespace chd.OpcUa.ServerWorker
{
    public class chdObjectSystemManager : ObjectSystemManager
    {
        protected override List<IUaServerObject> GetObjects()
        {
            var lst = new List<IUaServerObject>()
            {
                new Calculator("CHDCalc")
            };
            return lst;
        }
    }
}
