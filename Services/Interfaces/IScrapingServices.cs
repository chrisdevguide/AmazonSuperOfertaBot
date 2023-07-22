using AmazonApi.Models;
using AmazonSuperOfertaBot.Dtos;

namespace AmazonSuperOfertaBot.Services.Interfaces
{
    public interface IScrapingServices
    {
        Task<List<AmazonProduct>> Scrape(ScrapeRequestDto request);
        Task<List<AmazonProduct>> ScrapeCategories(ScrapeCategoriesRequestDto request);
        Task<AmazonProduct> ScrapeProduct(string asin);
    }
}