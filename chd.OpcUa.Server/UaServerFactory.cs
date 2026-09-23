using Opc.Ua.Server.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Opc.Ua;
using Opc.Ua.Server;

namespace chd.OpcUa.Server
{
    public class UaServerFactory(IServiceProvider serviceProvider) : IOpcUaServerFactory
    {
        public StandardServer CreateServer(ITelemetryContext telemetry, TimeProvider timeProvider)
        {
            return serviceProvider.GetRequiredService<UaServer>();
        }
    }
}
