namespace ElAhorrador.Services.Implementations
{
    public class StartupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IServiceScope scope = _serviceProvider.CreateScope();
            TelegramServices telegramServices = scope.ServiceProvider.GetRequiredService<TelegramServices>();
            telegramServices.StartBot();
            while (true)
            {
                await telegramServices.CheckAlerts();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
