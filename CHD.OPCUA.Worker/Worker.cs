using CHD.OPCUA.Client;
using CHD.OPCUA.Contracts.Interfaces;

namespace CHD.OPCUA.Worker
{
    public class Worker(ILogger<Worker> logger, IOpcUAClient client) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await client.StartAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                
                var val = await client.ReadAsync<float>("1:CC1001?Input1", stoppingToken);

                await client.WriteAsync("1:CC1001?Input1", ++val, stoppingToken);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await client.StopAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
