using AmazonApi.Data;
using ElAhorrador.Data.Repositories.Interfaces;
using ElAhorrador.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ElAhorrador.Data.Repositories.Implementations
{
    public class ConfigurationRepository : IConfigurationRepository
    {
        private readonly DataContext _dataContext;

        public ConfigurationRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<T> GetConfiguration<T>()
        {
            _dataContext.Logs.Add(new()
            {
                Type = "Test",
                Data = typeof(T).Name,

            });
            await _dataContext.SaveChangesAsync();
            Configuration configuration = await _dataContext.Configurations.FirstOrDefaultAsync(x => x.Name == typeof(T).Name);
            return JsonConvert.DeserializeObject<T>(configuration.Value);
        }
    }
}
