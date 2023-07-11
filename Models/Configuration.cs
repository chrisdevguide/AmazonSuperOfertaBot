using System.ComponentModel.DataAnnotations;

namespace ElAhorrador.Models
{
    public class Configuration
    {
        [Key]
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
