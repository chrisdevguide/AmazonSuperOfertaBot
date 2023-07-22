using AmazonApi.Data;
using AmazonSuperOfertaBot.Data.Repositories.Interfaces;
using AmazonSuperOfertaBot.Models;
using Microsoft.EntityFrameworkCore;

namespace AmazonSuperOfertaBot.Data.Repositories.Implementations
{
    public class AmazonCategoriesRepository : IAmazonCategoriesRepository
    {
        private readonly DataContext _dataContext;

        public AmazonCategoriesRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<AmazonCategory> GetAmazonCategory(string id)
        {
            return await _dataContext.AmazonCategories.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<AmazonCategory>> GetAmazonCategories()
        {
            return await _dataContext.AmazonCategories.ToListAsync();
        }

        public async Task CreateAmazonCategory(AmazonCategory amazonCategory)
        {
            _dataContext.AmazonCategories.Add(amazonCategory);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateAmazonCategory(AmazonCategory AmazonCategory)
        {
            _dataContext.AmazonCategories.Update(AmazonCategory);
            await _dataContext.SaveChangesAsync();
        }

        public async Task DeleteAmazonCategory(string id)
        {
            AmazonCategory AmazonCategory = await GetAmazonCategory(id);
            _dataContext.AmazonCategories.Remove(AmazonCategory);
            await _dataContext.SaveChangesAsync();
        }
    }
}
