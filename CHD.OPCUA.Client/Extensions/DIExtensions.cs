using CHD.OPCUA.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using CHD.OPCUA.Contracts.Options;
using Opc.Ua.Client.Subscriptions;

namespace CHD.OPCUA.Client.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddOpcUaClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpcUa();
            services.Configure<OpcUaClientOptions>(configuration.GetSection(nameof(OpcUaClientOptions)));
            services.Configure<SubscriptionOptions>(configuration.GetSection(nameof(SubscriptionOptions)));
            services.AddTransient<IOpcUAClient, OpcUaClient>();
            services.AddTransient<NotificationHandler>();

            return services;
        }
    }
}
