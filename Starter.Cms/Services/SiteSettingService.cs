using Microsoft.EntityFrameworkCore;
using Starter.Cms.Data;

namespace Starter.Cms.Services;

public interface ISiteSettingService
{
    Task<IDictionary<string, string>> GetAllAsync();
    Task<string> GetAsync(string key, string fallback = "");
}

public class SiteSettingService : ISiteSettingService
{
    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public SiteSettingService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IDictionary<string, string>> GetAllAsync() =>
        _cache.GetOrCreateAsync<IDictionary<string, string>>($"{IContentCache.Prefix}settings", async () =>
            await _db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value));

    public async Task<string> GetAsync(string key, string fallback = "")
    {
        var all = await GetAllAsync();
        return all.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;
    }
}
