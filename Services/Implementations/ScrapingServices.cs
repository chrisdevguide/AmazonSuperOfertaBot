using AmazonApi.Models;
using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using ElAhorrador.Data.Repositories.Interfaces;
using ElAhorrador.Dtos;
using ElAhorrador.Extensions;
using ElAhorrador.Models;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text;

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
            HttpClient client = new();
            client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");

            GetHtmlDocumentRequest request = new()
            {
                id = "",
                name = "",
                errors = "",
                json = JsonConvert.SerializeObject(new JsonHtmlDocumentRequest()
                {
                    method = "GET",
                    url = url,
                    apiNode = "DE",
                    contentType = "",
                    headers = "",
                    errors = "",
                    curlCmd = $"curl {url}",
                    codeCmd = "",
                    jsonCmd = "",
                    xmlCmd = "",
                    lang = "",
                    auth = new()
                    {
                        auth = "",
                        bearerToken = "",
                        basicUsername = "",
                        basicPassword = "",
                        customHeader = "",
                        encrypted = ""
                    },
                    compare = false,
                    idnUrl = url
                }),
                sessionId = 0,
                deviceId = ""
            };

            HttpContent httpContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://apide.reqbin.com/api/v1/requests", httpContent);

            string html = response.Content.ReadFromJsonAsync<GetHtmlDocumentResponse>().Result.Content;

            await _logsRepository.CreateLog("Info HTML", html);

            HtmlDocument document = new();
            document.LoadHtml(html);
            return document;
        }

        public class GetHtmlDocumentResponse
        {
            public string Content { get; set; }

        }

        public class GetHtmlDocumentRequest
        {
            public string id { get; set; }
            public string name { get; set; }
            public string errors { get; set; }
            public string json { get; set; } // Object for the 'json' property
            public long sessionId { get; set; }
            public string deviceId { get; set; }
        }

        public class JsonHtmlDocumentRequest
        {
            public string method { get; set; }
            public string url { get; set; }
            public string apiNode { get; set; }
            public string contentType { get; set; }
            public string headers { get; set; }
            public string errors { get; set; }
            public string curlCmd { get; set; }
            public string codeCmd { get; set; }
            public string jsonCmd { get; set; }
            public string xmlCmd { get; set; }
            public string lang { get; set; }
            public AuthData auth { get; set; }
            public bool compare { get; set; }
            public string idnUrl { get; set; }
        }
    }

    public class AuthData
    {
        public string auth { get; set; }
        public string bearerToken { get; set; }
        public string basicUsername { get; set; }
        public string basicPassword { get; set; }
        public string customHeader { get; set; }
        public string encrypted { get; set; }
    }

}

