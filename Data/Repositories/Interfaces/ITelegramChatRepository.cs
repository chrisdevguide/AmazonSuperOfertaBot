using ElAhorrador.Models;

namespace ElAhorrador.Data.Repositories.Implementations
{
    public interface ITelegramChatRepository
    {
        Task CreateTelegramChat(TelegramChat telegramChat);
        Task DeleteTelegramChat(long id);
        Task<TelegramChat> GetTelegramChat(long id);
        Task UpdateTelegramChat(TelegramChat telegramChat);
    }
}