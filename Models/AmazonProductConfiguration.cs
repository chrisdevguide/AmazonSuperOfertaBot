namespace ElAhorrador.Models
{
    public abstract class AmazonProductConfiguration
    {
        public string SearchProductUrl { get; set; }
        public string AsinPath { get; set; }
        public string NamePath { get; set; }
        public string CurrentPricePath { get; set; }
        public string OriginalPricePath { get; set; }
        public string StarsPath { get; set; }
        public string ReviewsCountPath { get; set; }
        public string HasStockPath { get; set; }
        public string ImageUrlPath { get; set; }
    }
}
