using AmazonApi.Models;
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
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("authority", "www.amazon.es");
            request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            request.Headers.Add("accept-language", "en-GB,en-US;q=0.9,en;q=0.8,es;q=0.7");
            request.Headers.Add("cache-control", "max-age=0");
            request.Headers.Add("cookie", "ubid-acbes=259-0337921-0383406; session-id=261-3401301-9939462; csd-key=eyJ3YXNtVGVzdGVkIjp0cnVlLCJ3YXNtQ29tcGF0aWJsZSI6dHJ1ZSwid2ViQ3J5cHRvVGVzdGVkIjpmYWxzZSwidiI6MSwia2lkIjoiN2Q1YTY4Iiwia2V5IjoiQXR4VTF3WkJnQXlOcDAySjBGUnc1QjJMTFpPOEhMYlhUL05YVXJxMmtqeUExU3hDMHc2SWdXZDl4SE1Nek1VQUF2bEk3cmlyYUJsRVp2NHBUMUN1OVRFRkFHQWU0dE56anliOXlMVjVSTTJjRmlaUXVGNmNoSnM0Nm1zb1RHTEc1dS9wU0hJd2xxcHZ2YmZjTC9jcmppZFA4RVc5TW1La0JiWER1aUdaSS91dnJ5SG5Bd1YvOUxOaGJ4eTFVaGs5RXdkRmxXcDRMSTQrSUkrM09VdjlPTDd5Y3lSc1AyTXI1b1kvRXlrNVljSC9DTExsZEtoUzdhenpIdnNZTFJ0aGpHVGZQQnZUTnR4NE9rU0VsdDAzbGp5dlFOVGpwK01XN1dtMmJmWENFc0hSSVlZL3ZraVVNQ2tOV0RtZisxNFdxbktyMEE4YjRNRGJneFFLK2UwTWpBPT0ifQ==; s_vnum=2113492896449%26vn%3D3; s_nr=1689171164448-Repeat; s_dslv=1689171164450; s_fid=6E7F2A98F939FF92-17FB949F197F08CA; sst-acbes=Sst1|PQGvh7IDiNRLV4o_ZxqTpYQ7B8ToqxDcu9oU4MHW0JvTEntNm9FAJAH_quxEhI7ikJojNXEOPCPqqQ7kuL8Z41qxy7U3tWw0CjHNZ53yuSY722Tcn2bXzMUB-UTQx8qvzmhkEmrvi816k2ILgfTGqE2iK25OLiwB5JJsoUrjSNU735obGFPaliW8CfPOYgyBFz55XRWk4o8qcOu9-uMZbCQ73xXl0ipAqQsl1DkA7HlRmTvOybMLFJKPw8VwxbblQU6C; session-id-time=2082787201l; i18n-prefs=EUR; lc-acbes=es_ES; session-token=\"0CFwBQVjwG54UTr7hZ2C6ZIIzRpEawYUL+F+qsdmRcR8B6kNcBvf91KqmLs/pum72Ggr9lZqDFy8BGUcwSVPHrUs07FlCG9PJJqlFU0qC6SAdr+KliK4jF5pnxeiCNmHHYKQRrTThcxA5ue3uNpWsU4QOssaqMu47kHCaOyCPQGQHKDhQOMjK1nFP7T4h5mZSvDBLTwxNbTIRpJIHKbv28QTjWDGgUIOKo4il8Zq5LI=\"; csm-hit=adb:adblk_yes&t:1689525816354&tb:YWE6D36KYNZ01KZ4DTDN+s-D9ZA1EEM79W03HA66MKN|1689525816354; i18n-prefs=EUR; session-id=260-4920659-2483311; session-id-time=2082787201l; session-token=\"Zj2CruzvCcwSEfuis1q6TzRdaoCsXkQUlX00ojS2+MLseuGhC16SCK4s4lwp+wenAftdJIu6MfITFYkYOfQ4TQZ0igwvJNPiE4SmTflRENPKhXJCuDdQB9wTauiQ2aeawreVe8cAHlBfYkdNVwOcEZ15aF2u3VAcrrbJVfs+s1cXGYJkiSNl+XUwtw5MXsSREQAz2EEiz4OoE8EpIxUe61LnPqQn4+FFoZ8Y2hezW18=\"; ubid-acbes=260-0829677-4134123");
            request.Headers.Add("device-memory", "8");
            request.Headers.Add("downlink", "4.55");
            request.Headers.Add("dpr", "1");
            request.Headers.Add("ect", "4g");
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
            request.Headers.Add("sec-fetch-site", "none");
            request.Headers.Add("sec-fetch-user", "?1");
            request.Headers.Add("upgrade-insecure-requests", "1");
            //request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
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
