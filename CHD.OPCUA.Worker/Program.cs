using CHD.OPCUA.Client;
using CHD.OPCUA.Client.Extensions;
using CHD.OPCUA.Contracts.Interfaces;
using CHD.OPCUA.Contracts.Options;
using CHD.OPCUA.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOpcUaClient(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
