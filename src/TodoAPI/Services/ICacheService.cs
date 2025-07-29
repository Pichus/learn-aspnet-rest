namespace TodoAPI.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key)
        where T : class;

    Task SetAsync<T>(string key, T value)
        where T : class;

    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory)
        where T : class;

    Task RemoveAsync(string key);
}