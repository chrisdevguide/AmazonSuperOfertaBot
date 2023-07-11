using System.ComponentModel.DataAnnotations;

namespace ElAhorrador.Models
{
    public class TelegramChat
    {
        [Key]
        public long Id { get; set; }
        public string ChatCommand { get; set; }
        public int ChatStep { get; set; }
        public string Data { get; set; }
        public DateTime StartedTime { get; set; } = DateTime.UtcNow;
    }
}
