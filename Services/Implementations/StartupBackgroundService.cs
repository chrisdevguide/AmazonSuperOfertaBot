using Quartz;

namespace ElAhorrador.Services.Implementations
{
    public class StartupBackgroundService : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            IServiceScope scope = _serviceProvider.CreateScope();
            TelegramServices telegramServices = scope.ServiceProvider.GetRequiredService<TelegramServices>();
            telegramServices.StartBot();
            await telegramServices.CheckAlerts();
        }

    }
}
