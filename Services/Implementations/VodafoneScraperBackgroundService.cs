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
        public async Task Execute(IJobExecutionContext context)
        {
            IServiceScope scope = _serviceProvider.CreateScope();
            TelegramServices telegramServices = scope.ServiceProvider.GetRequiredService<TelegramServices>();
            List<string> urls = new()
            {
                "https://www.vodafone.es/c/srv/vf-back-catalogo/api/ftol/terminal/terminaldetail/?clientType=0&shopType=7&registerType=2&sceneType=0&contractType=0&sap=315279&lineType=0&terminalType=8&flgAutoComplete=true&flgStockOnly=false&idList=251723936&showEvenWhitoutCheckCoverage=true&additionalLines=0",
                "https://www.vodafone.es/c/srv/vf-back-catalogo/api/ftol/terminal/terminaldetail/?clientType=0&shopType=7&registerType=2&sceneType=0&contractType=0&sap=315264&lineType=0&terminalType=3&flgAutoComplete=true&flgStockOnly=false&idList=251723936&showEvenWhitoutCheckCoverage=true&additionalLines=0"
            };
            long chatId = 6311333292;
            HttpClient http = new();

            urls.ForEach(async url =>
            {
                Welcome jsonResponse = await http.GetFromJsonAsync<Welcome>(url);
                if (!jsonResponse.ListTerminals.TrueForAll(x => x.ItemStock.Stock == 0)) await telegramServices.SendMessage($"{jsonResponse.Nombre} is available.", chatId);
            });

            if (DateTime.Now.Minute % 30 == 0) await telegramServices.SendMessage("System is working.", chatId); ;

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
