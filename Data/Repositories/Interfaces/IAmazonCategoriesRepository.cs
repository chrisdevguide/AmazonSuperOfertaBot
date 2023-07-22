using AmazonSuperOfertaBot.Models;

namespace AmazonSuperOfertaBot.Data.Repositories.Interfaces
{
    public interface IAmazonCategoriesRepository
    {
        Task CreateAmazonCategory(AmazonCategory amazonCategory);
        Task DeleteAmazonCategory(string id);
        Task<List<AmazonCategory>> GetAmazonCategories();
        Task<AmazonCategory> GetAmazonCategory(string id);
        Task UpdateAmazonCategory(AmazonCategory AmazonCategory);
    }
}