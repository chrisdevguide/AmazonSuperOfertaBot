namespace ElAhorrador.Models
{
    public class TelegramConfiguration
    {
        public string ApiKey { get; set; }
        public string ChannelName { get; set; }
        public string ScrapeCommand { get; set; }
        public string ScrapeProductCommand { get; set; }
        public string CreateAlertCommand { get; set; }
        public string EditAlertsCommand { get; set; }
        public string AdminCommand { get; set; }
        public string ExitCommand { get; set; }
        public byte[] AdminPassword { get; set; }
        public byte[] AdminPasswordHash { get; set; }
    }
}
