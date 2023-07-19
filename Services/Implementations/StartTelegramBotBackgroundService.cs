using Quartz;

namespace ElAhorrador.Services.Implementations
{
    public class StartTelegramBotBackgroundService : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public StartTelegramBotBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            IServiceScope scope = _serviceProvider.CreateScope();
            TelegramServices telegramServices = scope.ServiceProvider.GetRequiredService<TelegramServices>();
            telegramServices.StartBot();
            await Task.CompletedTask;
        }

    }
}
