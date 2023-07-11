using AmazonApi.Data;
using ElAhorrador.Models;
using Microsoft.EntityFrameworkCore;

namespace ElAhorrador.Data.Repositories.Implementations
{
    public class TelegramChatRepository : ITelegramChatRepository
    {
        private readonly DataContext _dataContext;

        public TelegramChatRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<TelegramChat> GetTelegramChat(long id)
        {
            return await _dataContext.TelegramChats.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task CreateTelegramChat(TelegramChat telegramChat)
        {
            _dataContext.TelegramChats.Add(telegramChat);
            await _dataContext.SaveChangesAsync();
        }

        public async Task UpdateTelegramChat(TelegramChat telegramChat)
        {
            _dataContext.TelegramChats.Update(telegramChat);
            await _dataContext.SaveChangesAsync();
        }

        public async Task DeleteTelegramChat(long id)
        {
            TelegramChat telegramChat = await GetTelegramChat(id);
            _dataContext.TelegramChats.Remove(telegramChat);
            await _dataContext.SaveChangesAsync();
        }


    }
}
