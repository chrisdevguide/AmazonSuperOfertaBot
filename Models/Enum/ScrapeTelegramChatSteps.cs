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
        AskForProductNameMustContainKeyword,
        ReadyToSimpleSearch,
        ReadyToAdvancedSearch,
    }
}
