using AmazonApi.Data;
using AmazonSuperOfertaBot.Models;
using Microsoft.EntityFrameworkCore;

namespace AmazonSuperOfertaBot.Data.Repositories.Implementations
{
    public class AmazonProductsTelegramRepository : IAmazonProductsTelegramRepository
    {
        private readonly DataContext _dataContext;

        public AmazonProductsTelegramRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<AmazonProductTelegram> GetAmazonProductTelegram(string asin)
        {
            return await _dataContext.AmazonProductsTelegram.FirstOrDefaultAsync(x => x.Asin == asin);
        }

        public async Task<List<AmazonProductTelegram>> GetAmazonProductsTelegram()
        {
            return await _dataContext.AmazonProductsTelegram.ToListAsync();
        }

        public async Task<List<AmazonProductTelegram>> GetAmazonProductsTelegramToSend()
        {
            return await _dataContext.AmazonProductsTelegram.Where(x => !x.SentToTelegram).ToListAsync();
        }

        public async Task CreateAmazonProductTelegram(AmazonProductTelegram amazonProductTelegram)
        {
            _dataContext.AmazonProductsTelegram.Add(amazonProductTelegram);
            await _dataContext.SaveChangesAsync();
        }

        public async Task AddAmazonProductsTelegram(List<AmazonProductTelegram> amazonProductsTelegram)
        {
            _dataContext.AmazonProductsTelegram.AddRange(amazonProductsTelegram);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateAmazonProductsTelegram(List<AmazonProductTelegram> amazonProductsTelegram)
        {
            _dataContext.AmazonProductsTelegram.UpdateRange(amazonProductsTelegram);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateAmazonProductTelegram(AmazonProductTelegram amazonProductTelegram)
        {
            _dataContext.AmazonProductsTelegram.Update(amazonProductTelegram);
            await _dataContext.SaveChangesAsync();
        }

        public async Task DeleteAmazonProductTelegram(string asin)
        {
            AmazonProductTelegram AmazonProductTelegram = await GetAmazonProductTelegram(asin);
            _dataContext.AmazonProductsTelegram.Remove(AmazonProductTelegram);
            await _dataContext.SaveChangesAsync();
        }
    }
}
