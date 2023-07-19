using Quartz;

namespace ElAhorrador.Services.Implementations
{
    public class CheckAmazonAlertsBackgroundService : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public CheckAmazonAlertsBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            IServiceScope scope = _serviceProvider.CreateScope();
            TelegramServices telegramServices = scope.ServiceProvider.GetRequiredService<TelegramServices>();
            await telegramServices.CheckAlerts();
        }

    }
}
