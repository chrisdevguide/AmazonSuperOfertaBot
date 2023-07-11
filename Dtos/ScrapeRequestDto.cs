using AmazonApi.Models;
using System.ComponentModel.DataAnnotations;

namespace ElAhorrador.Dtos
{
    public record ScrapeRequestDto(
        [Required] string SearchText,
        decimal MinimumDiscount = 0,
        decimal MinimumStars = 0,
        int MinimumReviews = 0,
        bool ProductsWithStock = true,
        bool MustContainSearchText = false,
        bool OrderByDiscount = true)
    {
        public bool FilterAmazonProduct(AmazonProduct amazonProduct)
        {
            if (amazonProduct.Discount < MinimumDiscount) return false;
            if (amazonProduct.Stars < MinimumStars) return false;
            if (amazonProduct.ReviewsCount < MinimumReviews) return false;
            if (ProductsWithStock && !amazonProduct.HasStock) return false;
            if (MustContainSearchText && !amazonProduct.Name.Contains(SearchText, StringComparison.InvariantCultureIgnoreCase)) return false;
            return true;

        }
    }
}
