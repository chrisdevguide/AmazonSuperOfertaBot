using System.ComponentModel.DataAnnotations;

namespace AmazonApi.Models
{
    public class AmazonProduct
    {
        [Key]
        public string Asin { get; set; }
        public string Name { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal Discount { get; set; }
        public List<decimal> PreviousPrices { get; set; } = new();
        public int ReviewsCount { get; set; }
        public decimal Stars { get; set; }
        public bool HasStock { get; set; }
        public DateTime InitialScrapedTime { get; set; } = DateTime.UtcNow;
        public DateTime LastScrapedTime { get; set; }
        public string ImageUrl { get; set; }
        public string ProductUrl { get; set; }

        public void CalculateDiscount()
        {
            if (OriginalPrice > 0 && CurrentPrice > 0)
            {
                decimal discount = ((OriginalPrice - CurrentPrice) / OriginalPrice) * 100;
                Discount = Math.Round(discount, 2);
            }
        }

        public bool IsValid() => !string.IsNullOrEmpty(Asin);
    }
}
