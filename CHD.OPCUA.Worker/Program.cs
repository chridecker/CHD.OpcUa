using CHD.OPCUA.Client;
using CHD.OPCUA.Contracts.Interfaces;
using CHD.OPCUA.Contracts.Options;
using CHD.OPCUA.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOpcUa();
builder.Services.Configure<OpcUaClientOptions>(builder.Configuration.GetSection(nameof(OpcUaClientOptions)));

builder.Services.AddTransient<IOpcUAClient, OpcUaClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
