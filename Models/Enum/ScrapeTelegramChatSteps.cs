namespace ElAhorrador.Models.Enum
{
    public enum ScrapeTelegramChatSteps
    {
        Started,
        AskForSearchType,
        AskForSearchTextSimpleSearch,
        AskForSearchTextAdvancedSearch,
        AskForMinimumDiscountAdvancedSearch,
        AskForMinimumStarsAdvancedSearch,
        AskForMinimumReviewsAdvancedSearch,
        ReadyToSimpleSearch,
        ReadyToAdvancedSearch,
    }
}
