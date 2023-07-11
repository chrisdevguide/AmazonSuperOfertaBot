using AmazonApi.Data;
using ElAhorrador.Models;
using Microsoft.EntityFrameworkCore;

namespace ElAhorrador.Data.Repositories.Implementations
{
    public class AmazonAlertRepository : IAmazonAlertRepository
    {
        private readonly DataContext _dataContext;

        public AmazonAlertRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<List<AmazonAlert>> GetAmazonAlerts() => await _dataContext.AmazonAlerts.ToListAsync();

        public async Task<List<AmazonAlert>> GetAmazonAlerts(long chatId) => await _dataContext.AmazonAlerts.Where(x => x.ChatId == chatId).ToListAsync();

        public async Task<AmazonAlert> GetAmazonAlert(Guid id) => await _dataContext.AmazonAlerts.FirstOrDefaultAsync(x => x.Id == id);

        public async Task<AmazonAlert> GetAmazonAlert(long chatId, string asin) => await _dataContext.AmazonAlerts.FirstOrDefaultAsync(x => x.ChatId == chatId && x.ProductAsin == asin);

        public async Task CreateAmazonAlert(AmazonAlert AmazonAlert)
        {
            _dataContext.AmazonAlerts.Add(AmazonAlert);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateAmazonAlert(AmazonAlert AmazonAlert)
        {
            _dataContext.AmazonAlerts.Update(AmazonAlert);
            await _dataContext.SaveChangesAsync();
        }

        public async Task DeleteAmazonAlert(Guid id)
        {
            AmazonAlert AmazonAlert = await GetAmazonAlert(id);
            _dataContext.AmazonAlerts.Remove(AmazonAlert);
            await _dataContext.SaveChangesAsync();
        }

        public async Task<bool> AmazonAlertExists(long chatId, string asin) => await _dataContext.AmazonAlerts.AnyAsync(x => x.ChatId == chatId && x.ProductAsin == asin);


    }
}
