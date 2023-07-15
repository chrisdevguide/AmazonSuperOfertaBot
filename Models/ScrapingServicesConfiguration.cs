namespace ElAhorrador.Models
{
    public class ScrapingServicesConfiguration
    {
        public string BaseProductUrl { get; set; }
        public string AffiliateName { get; set; }
        public string UserAgentHeader { get; set; }
        public List<string> Cookies { get; set; }
    }
}
