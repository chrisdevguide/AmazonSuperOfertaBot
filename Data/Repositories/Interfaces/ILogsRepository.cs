using AmazonSuperOfertaBot.Models;

namespace AmazonSuperOfertaBot.Data.Repositories.Interfaces
{
    public interface ILogsRepository
    {
        Task CreateLog(Log Log);
        Task DeleteLog(Guid id);
        Task<Log> GetLog(Guid id);
        Task UpdateLog(Log Log);
    }
}