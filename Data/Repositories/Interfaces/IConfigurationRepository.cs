namespace ElAhorrador.Data.Repositories.Interfaces
{
    public interface IConfigurationRepository
    {
        Task<T> GetConfiguration<T>();
    }
}