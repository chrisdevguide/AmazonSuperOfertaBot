namespace ElAhorrador.Models
{
    public class AmazonAlert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public long ChatId { get; set; }
        public string ProductAsin { get; set; }
        public List<decimal> Prices { get; set; } = new();
        public DateTime CreatedTime { get; set; }
    }
}
