using ElAhorrador.Services.Implementations;
using Quartz;

namespace AmazonSuperOfertaBot.Services.Implementations
{
    public class VodafoneScraperBackgroundService : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        public VodafoneScraperBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public Task Execute(IJobExecutionContext context)
        {
            IServiceScope scope = _serviceProvider.CreateScope();
            TelegramServices telegramServices = scope.ServiceProvider.GetRequiredService<TelegramServices>();
            List<string> urls = new()
            {
                "https://www.vodafone.es/c/srv/vf-back-catalogo/api/ftol/terminal/terminaldetail/?clientType=0&shopType=7&registerType=2&sceneType=0&contractType=0&sap=315279&lineType=0&terminalType=8&flgAutoComplete=true&flgStockOnly=false&idList=251723936&showEvenWhitoutCheckCoverage=true&additionalLines=0",
                "https://www.vodafone.es/c/srv/vf-back-catalogo/api/ftol/terminal/terminaldetail/?clientType=0&shopType=7&registerType=2&sceneType=0&contractType=0&sap=315264&lineType=0&terminalType=3&flgAutoComplete=true&flgStockOnly=false&idList=251723936&showEvenWhitoutCheckCoverage=true&additionalLines=0",
            };
            long chatId = 6311333292;
            HttpClient http = new();

            urls.ForEach(async url =>
            {
                Welcome jsonResponse = await http.GetFromJsonAsync<Welcome>(url);

                jsonResponse?.ListTerminals?.ForEach(async x =>
                {
                    if (x.ItemStock.Stock < 1) return;
                    await telegramServices.SendMessage($"{jsonResponse.Nombre} with color <b>'{x.Color}'</b> and capacity of <b>{x.Capacidad}</b> has <b>{x.ItemStock.Stock}</b> units available.", chatId);
                }
                );
            });

            if (DateTime.Now.Hour % 4 == 0)
            {
                telegramServices.SendMessage($"System is working.", chatId).RunSynchronously();
            }

            return Task.CompletedTask;
        }
    }

    public partial class Welcome
    {
        public List<ListTerminal> ListTerminals { get; set; }
        public string Nombre { get; set; }

    }

    public partial class ListTerminal
    {
        public ItemStock ItemStock { get; set; }
        public string Color { get; set; }
        public string Capacidad { get; set; }
    }

    public partial class ItemStock
    {
        public bool Visible { get; set; }
        public bool Presale { get; set; }
        public bool Unavailable { get; set; }
        public string AvaliableStockText { get; set; }
        public long Stock { get; set; }
        public bool Notification { get; set; }
        public bool Accelerator { get; set; }
    }

}
