using chd.OpcUa.Server.Interfaces;
using chd.OpcUa.Server.Options;
using chd.OpcUa.ServerWorker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using Opc.Ua.Server.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Server.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddOpcUaServer<TNamespaceManager, TSystemManager>(this IServiceCollection services,
            Action<ServerOptions> serverConfig = null,
            string configurationFile = "OpcUaServerConfig.xml")
            where TNamespaceManager : class, INamespaceManager
            where TSystemManager : UnderlyingSystemManager
        {
            var configFile = new FileInfo(configurationFile);
            if (!configFile.Exists)
            {
                throw new FileNotFoundException("Opc UA Server Config konnte nicht gefunden werden!", configFile.FullName);
            }

            if (serverConfig is not null)
            {
                services.Configure<ServerOptions>(serverConfig);
            }

            var server = services.AddOpcUa()
                 .AddServer(configFile.FullName);

            services.TryAddSingleton(TimeProvider.System);
            services.AddTransient<ITelemetryContext>(sp =>
                DefaultTelemetry.Create(c => c.SetMinimumLevel(LogLevel.Trace)));
            services.TryAddSingleton<UaServer>();
            services.TryAddSingleton<UaServerFactory>();
            services.TryAddSingleton<NodeManagerFactory>();
            services.Replace(ServiceDescriptor.Singleton<IAsyncNodeManagerFactory>(
                provider => provider.GetService<NodeManagerFactory>()));


            // the forms of the server samples show the running server and take the
            // shared StandardServer, so they do not have to know the server class.
            services.AddSingleton<StandardServer>(
                provider => provider.GetRequiredService<UaServer>());

            // the hosted server creates its server through this factory: the instance
            // of the sample from the container, which the main form resolves too.
            services.Replace(ServiceDescriptor.Singleton<IOpcUaServerFactory>(
                provider => provider.GetService<UaServerFactory>()));

            // the parts of the server: the node managers of the sample first of all.
            server.AddNodeManager<NodeManagerFactory>();

            // registered after the parts of the sample, so its startup tasks have run
            // when the start of the host returns with the server listening - which the
            // samples and their tests rely on.
            services.AddSingleton<UaServerStartup>();
            services.AddSingleton<IServerStartupTask, UaServerStartup>();
            services.AddSingleton<INamespaceManager, TNamespaceManager>();
            services.AddSingleton<IUnderlyingSystemManager, TSystemManager>();



            return services;
        }
    }
}
