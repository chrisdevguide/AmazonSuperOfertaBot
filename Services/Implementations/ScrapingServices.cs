using AmazonApi.Models;
using AmazonSuperOfertaBot.Data.Repositories.Implementations;
using ElAhorrador.Data.Repositories.Interfaces;
using ElAhorrador.Dtos;
using ElAhorrador.Extensions;
using ElAhorrador.Models;
using HtmlAgilityPack;
using ScrapySharp.Network;

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
            //CookieContainer cookieContainer = new();
            //_scrapingServicesConfiguration.Cookies.ForEach(x => cookieContainer.SetCookies(new Uri("https://www.amazon.es"), x));

            //HttpClientHandler handler = new()
            //{
            //    CookieContainer = cookieContainer,
            //    UseCookies = true
            //};


            //// Create an instance of HttpClient with the configured handler
            //HttpClient httpClient = new(handler);

            //List<string> list = new() { "ubid-acbes=259-0337921-0383406; session-id=261-3401301-9939462; csd-key=eyJ3YXNtVGVzdGVkIjp0cnVlLCJ3YXNtQ29tcGF0aWJsZSI6dHJ1ZSwid2ViQ3J5cHRvVGVzdGVkIjpmYWxzZSwidiI6MSwia2lkIjoiN2Q1YTY4Iiwia2V5IjoiQXR4VTF3WkJnQXlOcDAySjBGUnc1QjJMTFpPOEhMYlhUL05YVXJxMmtqeUExU3hDMHc2SWdXZDl4SE1Nek1VQUF2bEk3cmlyYUJsRVp2NHBUMUN1OVRFRkFHQWU0dE56anliOXlMVjVSTTJjRmlaUXVGNmNoSnM0Nm1zb1RHTEc1dS9wU0hJd2xxcHZ2YmZjTC9jcmppZFA4RVc5TW1La0JiWER1aUdaSS91dnJ5SG5Bd1YvOUxOaGJ4eTFVaGs5RXdkRmxXcDRMSTQrSUkrM09VdjlPTDd5Y3lSc1AyTXI1b1kvRXlrNVljSC9DTExsZEtoUzdhenpIdnNZTFJ0aGpHVGZQQnZUTnR4NE9rU0VsdDAzbGp5dlFOVGpwK01XN1dtMmJmWENFc0hSSVlZL3ZraVVNQ2tOV0RtZisxNFdxbktyMEE4YjRNRGJneFFLK2UwTWpBPT0ifQ==; s_vnum=2113492896449%26vn%3D3; s_nr=1689171164448-Repeat; s_dslv=1689171164450; s_fid=6E7F2A98F939FF92-17FB949F197F08CA; sst-acbes=Sst1|PQGvh7IDiNRLV4o_ZxqTpYQ7B8ToqxDcu9oU4MHW0JvTEntNm9FAJAH_quxEhI7ikJojNXEOPCPqqQ7kuL8Z41qxy7U3tWw0CjHNZ53yuSY722Tcn2bXzMUB-UTQx8qvzmhkEmrvi816k2ILgfTGqE2iK25OLiwB5JJsoUrjSNU735obGFPaliW8CfPOYgyBFz55XRWk4o8qcOu9-uMZbCQ73xXl0ipAqQsl1DkA7HlRmTvOybMLFJKPw8VwxbblQU6C; session-id-time=2082787201l; i18n-prefs=EUR; lc-acbes=es_ES; csm-hit=adb:adblk_yes&t:1689411439172&tb:DHNGMBXCDK7AK67FHQ8X+s-KHJVM49Z39D3N177S149|1689411439172; session-token=\"jUaXhaxf/Q6jY43RgXsNOQ7thfj0NOczQAi/D19X4ijS1PWjGfAGexkmJIQNH/BUqDoYFnCa4S24mv1pehPbn589uyaO9iOByyc6m581+bO9UmDFOtvyfdGtUNtOEhWmzyca621MKt+/3aFgdG2lIi91I08zMTqVSButL5+ZVjPLHj6wZKD7P9t96vEHF5D5LQQD/kDM893M2AEjU/AX9SIDV7wpiztsKJNGJG58o7U=\"" };

            //var a = JsonConvert.SerializeObject(list);

            //if (!string.IsNullOrEmpty(_scrapingServicesConfiguration.UserAgentHeader)) httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_scrapingServicesConfiguration.UserAgentHeader);

            //HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            //if (httpResponse.IsSuccessStatusCode)
            //{
            //    await _logsRepository.CreateLog("Info GetHtmlDocument", httpResponse);
            //}
            //else
            //{
            //    await _logsRepository.CreateLog("Error", httpResponse);
            //    return null;
            //}

            //string htmlPage = await httpResponse.Content.ReadAsStringAsync();
            //await _logsRepository.CreateLog("Info GetHtmlDocument html", htmlPage);
            //httpResponse.Headers.TryGetValues("Set-Cookie", out var cookieValues);

            //string headers = "";
            //foreach (var header in httpClient.DefaultRequestHeaders)
            //{
            //    headers += $"Key={header.Key}, Value={string.Join(", ", header.Value)}\n";
            //}

            //string cookies = "";
            //if (cookieValues is not null)
            //{
            //    foreach (var cookie in cookieValues)
            //    {
            //        cookies += $"Value={cookie}, ";
            //    }
            //}


            //await _logsRepository.CreateLog("Info GetHtmlDocument Headers", headers);
            //await _logsRepository.CreateLog("Info GetHtmlDocument Cookies", cookies);

            ScrapingBrowser scrapingBrowser = new();
            if (!string.IsNullOrEmpty(_scrapingServicesConfiguration.UserAgentHeader)) scrapingBrowser.Headers["User-Agent"] = _scrapingServicesConfiguration.UserAgentHeader;
            string htmlPage = scrapingBrowser.NavigateToPage(new Uri(url));

            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(htmlPage);

            return htmlDocument;
        }
    }
}
