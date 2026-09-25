using chd.OpcUa.Server;
using chd.OpcUa.Server.Extensions;
using chd.OpcUa.ServerWorker;

var builder = Host.CreateApplicationBuilder(args);

//builder.Services.AddOpcUaServer<NamespaceManager, chdSystemManager>();
builder.Services.AddOpcUaServer<NamespaceManager, chdObjectSystemManager>();


var host = builder.Build();
host.Run();
