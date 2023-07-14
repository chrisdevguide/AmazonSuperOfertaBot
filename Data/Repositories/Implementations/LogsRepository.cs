using AmazonApi.Data;
using AmazonSuperOfertaBot.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

        public async Task CreateLog(string type, object data)
        {
            _dataContext.Logs.Add(new() { Type = type, Data = JsonConvert.SerializeObject(data) });
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
