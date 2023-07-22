using ElAhorrador.Services.Implementations;
using Quartz;

namespace AmazonSuperOfertaBot.Services.Implementations
{
    public class SearchAmazonCategoriesBackgroundService : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public SearchAmazonCategoriesBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            IServiceScope scope = _serviceProvider.CreateScope();
            TelegramServices telegramServices = scope.ServiceProvider.GetRequiredService<TelegramServices>();
            await telegramServices.SearchAmazonCategories(40);
            await telegramServices.SendAmazonProductsTelegramToChannel();
        }

    }
}
