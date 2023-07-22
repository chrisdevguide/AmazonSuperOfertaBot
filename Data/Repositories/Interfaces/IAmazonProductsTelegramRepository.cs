using AmazonSuperOfertaBot.Models;

namespace AmazonSuperOfertaBot.Data.Repositories.Implementations
{
    public interface IAmazonProductsTelegramRepository
    {
        Task AddAmazonProductsTelegram(List<AmazonProductTelegram> amazonProductsTelegram);
        Task CreateAmazonProductTelegram(AmazonProductTelegram amazonProductTelegram);
        Task DeleteAmazonProductTelegram(string asin);
        Task<List<AmazonProductTelegram>> GetAmazonProductsTelegram();
        Task<List<AmazonProductTelegram>> GetAmazonProductsTelegramToSend();
        Task<AmazonProductTelegram> GetAmazonProductTelegram(string asin);
        Task UpdateAmazonProductsTelegram(List<AmazonProductTelegram> amazonProductsTelegram);
        Task UpdateAmazonProductTelegram(AmazonProductTelegram amazonProductTelegram);
    }
}