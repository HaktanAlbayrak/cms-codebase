using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace AmtVinc.Cms.Services;

public class CachingService : ICachingService
{
    private readonly IMemoryCache _cache;
    // Önek bazlı temizlik için anahtarları takip ederiz.
    private static readonly ConcurrentDictionary<string, byte> _keys = new();

    public CachingService(IMemoryCache cache) => _cache = cache;

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? duration = null)
    {
        if (_cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory();
        _cache.Set(key, value, duration ?? TimeSpan.FromMinutes(30));
        _keys.TryAdd(key, 0);
        return value;
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            Remove(key);
    }
}
