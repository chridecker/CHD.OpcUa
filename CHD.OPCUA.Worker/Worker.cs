using chd.OpcUa.Client;
using chd.OpcUa.Contracts.Interfaces;

namespace chd.OpcUa.Worker
{
    public class Worker(ILogger<Worker> logger, IOpcUAClient client) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            client.MonitoredItemNotification += Client_MonitoredItemNotification;
            await client.StartAsync(stoppingToken);
            //await client.MonitorItem("1:CC1001?Input1", 5000, stoppingToken);
            //await client.MonitorItem("1:CC1001?Input2", 5000, stoppingToken);
            await client.MonitorItem("2:State", 500, stoppingToken);
            var val = await client.ReadAsync<uint>("2:State", stoppingToken);

            var input = new ProcessStartInput(val, val == 0 ? val + (uint)10 : 0);

            var output = await client.CallMethod<ProcessStartInput, ProcessStartOutput>("2:Start", input, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {

                //var val = await client.ReadAsync<float>("1:CC1001?Input1", stoppingToken);

                //await client.WriteAsync("1:CC1001?Input1", ++val, stoppingToken);
                await Task.Delay(1000, stoppingToken);

                //client.RemoveMonitorItem("1:CC1001?Input2");
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
