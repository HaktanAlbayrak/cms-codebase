using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;

namespace AmtVinc.Cms.Services;

/// <summary>
/// <see cref="SiteSetting"/> anahtar-değer deposunu güçlü-tipli marka/tema/SEO bilgisine
/// dönüştürür. Arayüzdeki tüm "statik" veriler (logo, renk, iletişim, sosyal, SEO)
/// buradan beslenir — panelden değiştirilebilir (SaaS mantığı).
/// </summary>
public interface IBrandingService
{
    Task<BrandingInfo> GetAsync();
    /// <summary>Panelden gelen anahtar-değer çiftlerini upsert eder ve cache'i temizler.</summary>
    Task SaveAsync(IDictionary<string, string> values);
}

/// <summary>Tüm marka/tema/iletişim/SEO değerlerinin tek okunur görünümü.</summary>
public class BrandingInfo
{
    public required IReadOnlyDictionary<string, string> Raw { get; init; }

    private string Get(string key, string fallback = "") =>
        Raw.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

    // ── Marka ──
    public string CompanyName => Get("branding.companyName", "AMT Vinç Platform");
    public string? LogoUrl => Get("branding.logoUrl");                 // açık zemin (header)
    public string? LogoLightUrl => Get("branding.logoLightUrl");      // koyu zemin (footer)
    public string FaviconUrl => Get("branding.faviconUrl", "/favicon.ico");
    public string Address => Get("branding.address", "");
    public string WorkingHours => Get("branding.workingHours", "");

    // ── İletişim ──
    public string Phone => Get("contact.phone", "");
    public string Email => Get("contact.email", "");
    public string Whatsapp => Get("whatsapp.number", "");

    // ── Sosyal (boşsa gizlenir) ──
    public string Facebook => Get("social.facebook");
    public string Instagram => Get("social.instagram");
    public string Linkedin => Get("social.linkedin");
    public string Youtube => Get("social.youtube");

    // ── Tema renkleri (hex) ──
    public string ColorPrimary => Get("theme.primary", "#2563eb");
    public string ColorPrimaryDark => Get("theme.primaryDark", "#1e40af");
    public string ColorInk => Get("theme.ink", "#0f172a");

    // ── SEO ──
    public string? OgImageUrl => Get("seo.ogImageUrl");
}

public class BrandingService : IBrandingService
{
    private const string CacheKey = IContentCache.Prefix + "branding";

    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public BrandingService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<BrandingInfo> GetAsync() =>
        _cache.GetOrCreateAsync(CacheKey, async () =>
        {
            var dict = await _db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);
            return new BrandingInfo { Raw = dict };
        });

    public async Task SaveAsync(IDictionary<string, string> values)
    {
        var existing = await _db.SiteSettings.ToDictionaryAsync(s => s.Key);
        foreach (var (key, value) in values)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (existing.TryGetValue(key, out var setting))
                setting.Value = value ?? "";
            else
                _db.SiteSettings.Add(new SiteSetting { Key = key, Value = value ?? "", Group = GroupOf(key) });
        }
        await _db.SaveChangesAsync();
        _cache.RemoveByPrefix(IContentCache.Prefix);
    }

    private static string GroupOf(string key) =>
        key.Contains('.') ? key[..key.IndexOf('.')] : "general";
}
