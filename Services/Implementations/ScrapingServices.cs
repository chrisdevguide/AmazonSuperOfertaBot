using AmazonApi.Models;
using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using AmazonSuperOfertaBot.Data.Repositories.Interfaces;
using AmazonSuperOfertaBot.Dtos;
using AmazonSuperOfertaBot.Models;
using AmazonSuperOfertaBot.Models.Enum;
using AmazonSuperOfertaBot.Services.Interfaces;
using AutoMapper;
using ElAhorrador.Data.Repositories.Interfaces;
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
        private readonly IAmazonCategoriesRepository _amazonCategoriesRepository;
        private readonly IMapper _mapper;
        private readonly ScrapingServicesConfiguration _scrapingServicesConfiguration;

        public ScrapingServices(IConfigurationRepository configurationRepository, ILogsRepository logsRepository, IAmazonCategoriesRepository amazonCategoriesRepository,
            IMapper mapper)
        {
            _configurationRepository = configurationRepository;
            _logsRepository = logsRepository;
            _amazonCategoriesRepository = amazonCategoriesRepository;
            _mapper = mapper;
            _scrapingServicesConfiguration = configurationRepository.GetConfiguration<ScrapingServicesConfiguration>().Result;
        }

        public async Task<List<AmazonProduct>> Scrape(ScrapeRequestDto request)
        {
            string url = "";
            List<AmazonProduct> amazonProducts = new();
            if (string.IsNullOrEmpty(request.SearchText) || string.IsNullOrWhiteSpace(request.SearchText)) return amazonProducts;

            ScrapeConfiguration scrapeConfiguration = await _configurationRepository.GetConfiguration<ScrapeConfiguration>() ?? throw new ApiException("No configuration has been loaded.");

            url = request.ScrapeMethod switch
            {
                ScrapeMethod.Keyword => $"{scrapeConfiguration.SearchProductUrl}k={request.SearchText}",
                ScrapeMethod.Category => $"{scrapeConfiguration.SearchProductUrl}rh=n:{request.SearchText},p_72:831280031,p_8:{(request.MinimumDiscount > 0 ? $"{(int)request.MinimumDiscount}-" : "")}",
                _ => throw new NotImplementedException()
            };

            HtmlDocument htmlDocument = await GetHtmlDocument(url);
            if (htmlDocument is null) return null;

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
                ImageUrl = amazonProductNode.SelectSingleNode(scrapeProductConfiguration.ImageUrlPath)?.GetAttributeValue("src", "") ?? "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a9/Amazon_logo.svg/2560px-Amazon_logo.svg.png",
                ProductUrl = $"{_scrapingServicesConfiguration.BaseProductUrl}{asin}?tag={_scrapingServicesConfiguration.AffiliateName}"
            };

            amazonProduct.CalculateDiscount();

            if (!amazonProduct.IsValid()) return null;

            return amazonProduct;
        }

        public async Task<List<AmazonProduct>> ScrapeCategories(ScrapeCategoriesRequestDto request)
        {
            List<AmazonProduct> amazonProducts = new();
            List<AmazonCategory> amazonCategories = await _amazonCategoriesRepository.GetAmazonCategories();
            foreach (AmazonCategory amazonCategory in amazonCategories)
            {
                ScrapeRequestDto scrapeRequest = _mapper.Map<ScrapeRequestDto>(request);
                scrapeRequest.SearchText = amazonCategory.Id;
                scrapeRequest.ScrapeMethod = ScrapeMethod.Category;

                List<AmazonProduct> foundAmazonProducts = await Scrape(scrapeRequest);

                if (foundAmazonProducts is null) continue;

                foundAmazonProducts = foundAmazonProducts.FindAll(x => !amazonProducts.Any(y => y.Asin == x.Asin));

                amazonProducts.AddRange(foundAmazonProducts);
                await Task.Delay(1000);
            }
            return amazonProducts;
        }

        private async Task<HtmlDocument> GetHtmlDocument(string url)
        {
            HttpClient http = new();
            http.DefaultRequestHeaders.Add("x-pie-req-header-cookie", _scrapingServicesConfiguration.AmazonCookie);
            http.DefaultRequestHeaders.Add("x-pie-req-meta-method", "GET");
            http.DefaultRequestHeaders.Add("x-pie-req-meta-url", url);

            HttpResponseMessage response = await http.PostAsync(_scrapingServicesConfiguration.ProxyUrl, null);

            if (!response.IsSuccessStatusCode)
            {
                await _logsRepository.CreateLog($"Error {nameof(GetHtmlDocument)}", response);
                return null;
            }

            string html = await response.Content.ReadAsStringAsync();

            HtmlDocument document = new();
            document.LoadHtml(html);
            return document;
        }

    }
}
