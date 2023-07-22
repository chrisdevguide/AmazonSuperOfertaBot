using System.ComponentModel.DataAnnotations;

namespace AmazonSuperOfertaBot.Models
{
    public class AmazonProductTelegram
    {
        [Key]
        public string Asin { get; set; }
        public decimal LastPrice { get; set; }
        public DateTime LastSearchedTime { get; set; } = DateTime.UtcNow;
        public bool SentToTelegram { get; set; } = false;
        public DateTime LastSentTime { get; set; }
    }
}
