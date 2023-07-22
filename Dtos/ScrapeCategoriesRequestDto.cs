namespace AmazonSuperOfertaBot.Dtos
{
    public record ScrapeCategoriesRequestDto(
        decimal MinimumDiscount = 0,
        decimal MinimumStars = 0,
        int MinimumReviews = 0,
        bool ProductsWithStock = true,
        bool MustContainSearchText = false,
        bool OrderByDiscount = true);

}