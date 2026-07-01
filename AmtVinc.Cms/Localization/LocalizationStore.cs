using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Localization;

/// <summary>
/// Singleton metin deposu. DB erişimi için scope açar, sonuçları cache'ler.
/// Admin metin güncellediğinde <see cref="Invalidate"/> ile tazelenir.
/// </summary>
public class LocalizationStore : ILocalizationStore
{
    private const string CachePrefix = "loc:";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICachingService _cache;

    public LocalizationStore(IServiceScopeFactory scopeFactory, ICachingService cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public Task<IReadOnlyDictionary<string, string>> GetAllAsync(string culture) =>
        _cache.GetOrCreateAsync<IReadOnlyDictionary<string, string>>(CachePrefix + culture, async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dict = await db.LocalizationResources
                .Where(r => r.LanguageCode == culture)
                .ToDictionaryAsync(r => r.Key, r => r.Value);
            return dict;
        }, TimeSpan.FromHours(6));

    public string Get(string culture, string key)
    {
        var dict = GetAllAsync(culture).GetAwaiter().GetResult();
        if (dict.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            return value;

        // İstenen dilde anahtar yoksa varsayılan dile düş (anahtarı göstermek yerine).
        var fallback = CultureContext.Default;
        if (!string.Equals(culture, fallback, StringComparison.OrdinalIgnoreCase))
        {
            var defaultDict = GetAllAsync(fallback).GetAwaiter().GetResult();
            if (defaultDict.TryGetValue(key, out var dv) && !string.IsNullOrEmpty(dv))
                return dv;
        }
        return key;
    }

    public void Invalidate() => _cache.RemoveByPrefix(CachePrefix);
}
