using AmazonSuperOfertaBot.Models;
using ElAhorrador.Models;
using Microsoft.EntityFrameworkCore;

namespace AmazonApi.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<TelegramChat> TelegramChats { get; set; }
        public DbSet<AmazonAlert> AmazonAlerts { get; set; }
        public DbSet<Log> Logs { get; set; }
    }
}
