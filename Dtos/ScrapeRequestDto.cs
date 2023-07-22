using AmazonApi.Models;
using AmazonSuperOfertaBot.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace AmazonSuperOfertaBot.Dtos
{
    public class ScrapeRequestDto
    {
        [Required]
        public string SearchText { get; set; }
        public ScrapeMethod ScrapeMethod { get; set; } = ScrapeMethod.Keyword;
        public decimal MinimumDiscount { get; set; } = 0;
        public decimal MinimumStars { get; set; } = 0;
        public int MinimumReviews { get; set; } = 0;
        public bool ProductsWithStock { get; set; } = true;
        public bool MustContainSearchText { get; set; } = false;
        public bool OrderByDiscount { get; set; } = true;

        public ScrapeRequestDto()
        {
            // Parameterless constructor for AutoMapper
        }

        public ScrapeRequestDto(string searchText)
        {
            SearchText = searchText;
        }

        public bool FilterAmazonProduct(AmazonProduct amazonProduct)
        {
            if (amazonProduct.Discount < MinimumDiscount) return false;
            if (amazonProduct.Stars < MinimumStars) return false;
            if (amazonProduct.ReviewsCount < MinimumReviews) return false;
            if (ProductsWithStock && !amazonProduct.HasStock) return false;
            if (MustContainSearchText && !SearchText.Split().ToList().TrueForAll(s => amazonProduct.Name.Contains(s, StringComparison.InvariantCultureIgnoreCase))) return false;
            return true;
        }
    }
}
