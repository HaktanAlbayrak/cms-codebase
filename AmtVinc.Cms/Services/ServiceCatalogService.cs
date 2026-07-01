using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.ViewModels;

namespace AmtVinc.Cms.Services;

/// <summary>Firma hizmetlerini (Vinç/Platform Kiralama vb.) kültüre göre döndürür.</summary>
public interface IServiceCatalogService
{
    Task<IReadOnlyList<ServiceVm>> GetAllAsync(string culture);
    Task<ServiceVm?> GetBySlugAsync(string slug, string culture);
}

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public ServiceCatalogService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<ServiceVm>> GetAllAsync(string culture) =>
        _cache.GetOrCreateAsync<IReadOnlyList<ServiceVm>>($"{IContentCache.Prefix}services:{culture}", async () =>
        {
            var services = await _db.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Include(s => s.Translations)
                .AsNoTracking().ToListAsync();

            return services.Select(s => Map(s, culture)).ToList();
        });

    public Task<ServiceVm?> GetBySlugAsync(string slug, string culture) =>
        _cache.GetOrCreateAsync<ServiceVm?>($"{IContentCache.Prefix}service:{slug}:{culture}", async () =>
        {
            var s = await _db.Services
                .Where(x => x.IsActive && x.Slug == slug)
                .Include(x => x.Translations)
                .AsNoTracking().FirstOrDefaultAsync();
            return s is null ? null : Map(s, culture);
        });

    private static ServiceVm Map(Domain.Service s, string culture)
    {
        var t = CultureContext.Pick(s.Translations, x => x.LanguageCode, culture);
        var title = t?.Title ?? s.Slug;
        return new ServiceVm(
            s.Slug,
            s.Icon,
            s.ImageUrl,
            title,
            t?.Summary ?? "",
            t?.Body ?? "",
            string.IsNullOrWhiteSpace(t?.MetaTitle) ? title : t!.MetaTitle!,
            t?.MetaDescription ?? t?.Summary ?? "");
    }
}
