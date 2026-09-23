using chd.OpcUa.Server.Options;
using Microsoft.Extensions.Options;
using Opc.Ua;
using Opc.Ua.Server.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server
{
    public class UaServer : DependencyInjectionStandardServer
    {
        private readonly ServerOptions _serverOptions;

        public UaServer(IOptions<ServerOptions> options, IServiceProvider services, ITelemetryContext telemetry, TimeProvider timeProvider) : base(services, telemetry, timeProvider)
        {
            _serverOptions = options.Value;
        }
        protected override ServerProperties LoadServerProperties()
        {
            ApplicationConfiguration configuration = Configuration;

            return new ServerProperties
            {
                ManufacturerName = _serverOptions.ManufacturerName,
                ProductName = configuration.ApplicationName,
                ProductUri = configuration.ProductUri,
                SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
                BuildNumber = Utils.GetAssemblyBuildNumber(),
                BuildDate = Utils.GetAssemblyTimestamp(),
            };
        }
    }
}
