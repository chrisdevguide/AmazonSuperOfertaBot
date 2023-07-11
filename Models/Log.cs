namespace AmazonSuperOfertaBot.Models
{
    public class Log
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}
