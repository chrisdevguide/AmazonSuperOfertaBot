﻿using AmazonApi.Models;
using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using ElAhorrador.Data.Repositories.Interfaces;
using ElAhorrador.Dtos;
using ElAhorrador.Extensions;
using ElAhorrador.Models;
using HtmlAgilityPack;

namespace AmazonApi.Services.Implementations
{
    public class ScrapingServices : IScrapingServices
    {
        private const string _euroSymbol = "€";
        private readonly IConfigurationRepository _configurationRepository;
        private readonly ILogsRepository _logsRepository;
        private readonly ScrapingServicesConfiguration _scrapingServicesConfiguration;

        public ScrapingServices(IConfigurationRepository configurationRepository, ILogsRepository logsRepository)
        {
            _configurationRepository = configurationRepository;
            _logsRepository = logsRepository;
            _scrapingServicesConfiguration = configurationRepository.GetConfiguration<ScrapingServicesConfiguration>().Result;
        }

        public async Task<List<AmazonProduct>> Scrape(ScrapeRequestDto request)
        {
            List<AmazonProduct> amazonProducts = new();
            if (string.IsNullOrEmpty(request.SearchText) || string.IsNullOrWhiteSpace(request.SearchText)) return amazonProducts;

            ScrapeConfiguration scrapeConfiguration = await _configurationRepository.GetConfiguration<ScrapeConfiguration>() ?? throw new ApiException("No configuration has been loaded.");

            HtmlDocument htmlDocument = await GetHtmlDocument($"{scrapeConfiguration.SearchProductUrl}{request.SearchText}");
            if (htmlDocument is null) return null;

            await _logsRepository.CreateLog("Info HtmlDocument Loaded", htmlDocument.ParsedText);

            HtmlNodeCollection amazonProductNodes = htmlDocument.DocumentNode.SelectNodes(scrapeConfiguration.ProductsPath);
            if (amazonProductNodes is null) return null;

            foreach (HtmlNode amazonProductNode in amazonProductNodes)
            {
                var a = amazonProductNode.SelectSingleNode(scrapeConfiguration.CurrentPricePath)?.InnerText;
                AmazonProduct amazonProduct = new()
                {
                    Asin = amazonProductNode.GetAttributeValue(scrapeConfiguration.AsinPath, null),
                    Name = amazonProductNode.SelectSingleNode(scrapeConfiguration.NamePath)?.InnerText.Trim(),
                    CurrentPrice = AmazonProduct.ParsePrice(amazonProductNode.SelectSingleNode(scrapeConfiguration.CurrentPricePath)?.InnerText),
                    OriginalPrice = AmazonProduct.ParsePrice(amazonProductNode.SelectSingleNode(scrapeConfiguration.OriginalPricePath)?.InnerText.Split()[0]),
                    Stars = decimal.TryParse(amazonProductNode.SelectSingleNode(scrapeConfiguration.StarsPath)?.InnerText[0..3].ReplaceCommaForDot(), out decimal stars) ? stars : 0,
                    ReviewsCount = int.TryParse(amazonProductNode.SelectSingleNode(scrapeConfiguration.ReviewsCountPath)?.InnerText.Replace(".", ""), out int reviewsCount) ? reviewsCount : 0,
                    HasStock = amazonProductNode.SelectSingleNode(scrapeConfiguration.HasStockPath) is null,
                    ImageUrl = amazonProductNode.SelectSingleNode(scrapeConfiguration.ImageUrlPath)?.GetAttributeValue("src", null),
                };

                amazonProduct.ProductUrl = $"{_scrapingServicesConfiguration.BaseProductUrl}{amazonProduct.Asin}?tag={_scrapingServicesConfiguration.AffiliateName}";
                amazonProduct.CalculateDiscount();

                if (!amazonProduct.IsValid()) continue;
                if (!request.FilterAmazonProduct(amazonProduct)) continue;
                amazonProducts.Add(amazonProduct);
            }

            if (request.OrderByDiscount)
            {
                amazonProducts = amazonProducts.OrderByDescending(x => x.Discount).ToList();
            }

            return amazonProducts;
        }

        public async Task<AmazonProduct> ScrapeProduct(string asin)
        {
            if (string.IsNullOrEmpty(asin) || string.IsNullOrWhiteSpace(asin)) return null;

            ScrapeProductConfiguration scrapeProductConfiguration = await _configurationRepository.GetConfiguration<ScrapeProductConfiguration>() ?? throw new ApiException("No configuration has been loaded.");
            HtmlDocument htmlDocument = await GetHtmlDocument($"{scrapeProductConfiguration.SearchProductUrl}{asin}");
            if (htmlDocument is null) return null;
            HtmlNode amazonProductNode = htmlDocument.DocumentNode.SelectSingleNode(scrapeProductConfiguration.ProductPath);
            if (amazonProductNode is null) return null;

            AmazonProduct amazonProduct = new()
            {
                Asin = amazonProductNode.SelectSingleNode(scrapeProductConfiguration.AsinPath.Replace("''", $"'{asin}'")) is null ? null : asin,
                Name = amazonProductNode.SelectSingleNode(scrapeProductConfiguration.NamePath)?.InnerText.Trim(),
                CurrentPrice = AmazonProduct.ParsePrice(amazonProductNode.SelectSingleNode(scrapeProductConfiguration.CurrentPricePath)?.InnerText.Split(_euroSymbol)[0]),
                OriginalPrice = AmazonProduct.ParsePrice(amazonProductNode.SelectSingleNode(scrapeProductConfiguration.OriginalPricePath)?.InnerText.Split(_euroSymbol)[0]),
                Stars = decimal.TryParse(amazonProductNode.SelectSingleNode(scrapeProductConfiguration.StarsPath)?.InnerText[0..3].ReplaceCommaForDot(), out decimal stars) ? stars : 0,
                ReviewsCount = int.TryParse(amazonProductNode.SelectSingleNode(scrapeProductConfiguration.ReviewsCountPath)?.InnerText.Split()[0].Replace(".", ""), out int reviewsCount) ? reviewsCount : 0,
                HasStock = amazonProductNode.SelectSingleNode(scrapeProductConfiguration.HasStockPath) is null,
                ImageUrl = amazonProductNode.SelectSingleNode(scrapeProductConfiguration.ImageUrlPath)?.GetAttributeValue("src", null),
                ProductUrl = $"{_scrapingServicesConfiguration.BaseProductUrl}{asin}?tag={_scrapingServicesConfiguration.AffiliateName}"
            };

            amazonProduct.CalculateDiscount();

            if (!amazonProduct.IsValid()) return null;

            return amazonProduct;
        }

        private async Task<HtmlDocument> GetHtmlDocument(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.amazon.es/s?k=logitech");
            request.Headers.Add("authority", "www.amazon.es");
            request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            request.Headers.Add("accept-language", "en-GB,en-US;q=0.9,en;q=0.8,es;q=0.7");
            request.Headers.Add("cache-control", "no-cache");
            request.Headers.Add("cookie", _scrapingServicesConfiguration.Cookie);
            request.Headers.Add("device-memory", "8");
            request.Headers.Add("downlink", "3.55");
            request.Headers.Add("dpr", "1");
            request.Headers.Add("ect", "4g");
            request.Headers.Add("pragma", "no-cache");
            request.Headers.Add("rtt", "50");
            request.Headers.Add("sec-ch-device-memory", "8");
            request.Headers.Add("sec-ch-dpr", "1");
            request.Headers.Add("sec-ch-ua", "\"Not.A/Brand\";v=\"8\", \"Chromium\";v=\"114\", \"Google Chrome\";v=\"114\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("sec-ch-ua-platform-version", "\"15.0.0\"");
            request.Headers.Add("sec-ch-viewport-width", "1519");
            request.Headers.Add("sec-fetch-dest", "document");
            request.Headers.Add("sec-fetch-mode", "navigate");
            request.Headers.Add("sec-fetch-site", "same-origin");
            request.Headers.Add("sec-fetch-user", "?1");
            request.Headers.Add("upgrade-insecure-requests", "1");
            request.Headers.Add("viewport-width", "1519");
            var response = await client.SendAsync(request);


            string html = await response.Content.ReadAsStringAsync();

            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(html);
            await _logsRepository.CreateLog("Info GetHtmlDocument Html", html);
            return htmlDocument;
        }
    }
}
