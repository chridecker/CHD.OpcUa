using chd.OpcUa.Contracts.Interfaces;
using chd.OpcUa.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client.Subscriptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace chd.OpcUa.Client.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddOpcUaClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOpcUa();
            services.Configure<OpcUaClientOptions>(configuration.GetSection(nameof(OpcUaClientOptions)));
            services.Configure<SubscriptionOptions>(configuration.GetSection(nameof(SubscriptionOptions)));
            services.AddTransient<ITelemetryContext>(sp =>
                DefaultTelemetry.Create(c => c.SetMinimumLevel(LogLevel.Trace)));
            services.AddTransient<IOpcUAClient, OpcUaClient>();
            services.AddTransient<NotificationHandler>();

            return services;
        }
    }
}
