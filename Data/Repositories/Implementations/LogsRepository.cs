using AmazonApi.Data;
using AmazonSuperOfertaBot.Data.Repositories.Interfaces;
using AmazonSuperOfertaBot.Models;
using Microsoft.EntityFrameworkCore;

namespace AmazonSuperOfertaBot.Data.Repositories.Implementations
{
    public class LogsRepository : ILogsRepository
    {
        private readonly DataContext _dataContext;

        public LogsRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<Log> GetLog(Guid id)
        {
            return await _dataContext.Logs.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task CreateLog(Log Log)
        {
            _dataContext.Logs.Add(Log);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateLog(Log Log)
        {
            _dataContext.Logs.Update(Log);
            await _dataContext.SaveChangesAsync();
        }

        public async Task DeleteLog(Guid id)
        {
            Log Log = await GetLog(id);
            _dataContext.Logs.Remove(Log);
            await _dataContext.SaveChangesAsync();
        }
    }
}
