namespace apiContact.Services
{
    /// <summary>
    /// Abstraction over the caching layer.
    /// Backed by Redis when a connection string is configured;
    /// falls back to IMemoryCache so the app works without any external infrastructure.
    /// </summary>
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
    }
}
