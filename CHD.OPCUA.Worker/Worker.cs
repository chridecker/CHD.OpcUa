using CHD.OPCUA.Client;
using CHD.OPCUA.Contracts.Interfaces;

namespace CHD.OPCUA.Worker
{
    public class Worker(ILogger<Worker> logger, IOpcUAClient client) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            client.MonitoredItemNotification += Client_MonitoredItemNotification;
            await client.StartAsync(stoppingToken);
            await client.MonitorItem("1:CC1001?Input1", 500, stoppingToken);
            await client.MonitorItem("1:CC1001?Input2", 500, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {

                var val = await client.ReadAsync<float>("1:CC1001?Input1", stoppingToken);



                //await client.WriteAsync("1:CC1001?Input1", ++val, stoppingToken);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task Client_MonitoredItemNotification(object? sender, Contracts.MonitoredItemEventArgs e)
        {
            logger?.LogInformation($"Item {e.Node} [{e.Value}]");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await client.StopAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
