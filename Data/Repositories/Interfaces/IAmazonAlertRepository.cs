using ElAhorrador.Models;

namespace ElAhorrador.Data.Repositories.Implementations
{
    public interface IAmazonAlertRepository
    {
        Task<bool> AmazonAlertExists(long chatId, string asin);
        Task CreateAmazonAlert(AmazonAlert AmazonAlert);
        Task DeleteAmazonAlert(Guid id);
        Task<AmazonAlert> GetAmazonAlert(Guid id);
        Task<AmazonAlert> GetAmazonAlert(long chatId, string asin);
        Task<List<AmazonAlert>> GetAmazonAlerts();
        Task<List<AmazonAlert>> GetAmazonAlerts(long chatId);
        Task UpdateAmazonAlert(AmazonAlert AmazonAlert);
    }
}