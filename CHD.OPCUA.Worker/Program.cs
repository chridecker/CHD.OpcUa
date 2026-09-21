using chd.OpcUa.Client;
using chd.OpcUa.Client.Extensions;
using chd.OpcUa.Contracts.Interfaces;
using chd.OpcUa.Contracts.Options;
using chd.OpcUa.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOpcUaClient(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
