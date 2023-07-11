using AmazonApi.Models;
using ElAhorrador.Dtos;

namespace AmazonApi.Services.Implementations
{
    public interface IScrapingServices
    {
        Task<List<AmazonProduct>> Scrape(ScrapeRequestDto request);
        Task<AmazonProduct> ScrapeProduct(string asin);
    }
}