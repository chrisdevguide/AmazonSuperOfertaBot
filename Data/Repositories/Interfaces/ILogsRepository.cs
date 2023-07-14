using AmazonSuperOfertaBot.Models;

namespace AmazonSuperOfertaBot.Data.Repositories.Implementations
{
    public interface ILogsRepository
    {
        Task CreateLog(string type, object data);
        Task DeleteLog(Guid id);
        Task<Log> GetLog(Guid id);
        Task UpdateLog(Log Log);
    }
}