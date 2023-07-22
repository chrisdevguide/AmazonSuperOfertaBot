using AmazonSuperOfertaBot.Dtos;
using AmazonSuperOfertaBot.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AmazonApi.Controllers
{
    public class ScrapingController : BaseController
    {
        private readonly IScrapingServices _scrapingServices;

        public ScrapingController(IScrapingServices scrapingServices)
        {
            _scrapingServices = scrapingServices;
        }

        [HttpGet]
        public async Task<ActionResult> Scrape([FromQuery][Required] ScrapeRequestDto request)
        {
            return Ok(await _scrapingServices.Scrape(request));
        }

        [HttpGet]
        public async Task<ActionResult> ScrapeProduct(string asin)
        {
            return Ok(await _scrapingServices.ScrapeProduct(asin));
        }

        [HttpGet]
        public async Task<ActionResult> ScrapeCategories([FromQuery][Required] ScrapeCategoriesRequestDto request)
        {
            return Ok(await _scrapingServices.ScrapeCategories(request));
        }

    }
}
