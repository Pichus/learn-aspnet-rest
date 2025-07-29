using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace TodoAPI.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        var cachedValue = await _distributedCache.GetStringAsync(key);

        if (cachedValue is null) return null;

        var value = JsonSerializer.Deserialize<T>(cachedValue);

        return value;
    }

    public async Task SetAsync<T>(string key, T value) where T : class
    {
        var cacheValue = JsonSerializer.Serialize(value);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
        };
        
        await _distributedCache.SetStringAsync(key, cacheValue, options);
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory) where T : class
    {
        var cachedValue = await GetAsync<T>(key);

        if (cachedValue is not null) return cachedValue;

        var value = await factory();

        if (value is null)
        {
            return null;
        }

        await SetAsync(key, value);

        return value;
    }

    public async Task RemoveAsync(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }
}