using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.ViewModels;

namespace AmtVinc.Cms.Services;

/// <summary>Makine filosunu (kategoriler + makineler) kültüre göre döndürür.</summary>
public interface IMachineService
{
    /// <summary>Aktif kategoriler ve içlerindeki aktif makineler (boş kategoriler atlanır).</summary>
    Task<IReadOnlyList<MachineCategoryVm>> GetCatalogAsync(string culture);
    Task<MachineVm?> GetBySlugAsync(string slug, string culture);
    Task<IReadOnlyList<MachineVm>> GetFeaturedAsync(string culture, int take = 6);
}

public class MachineService : IMachineService
{
    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public MachineService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<MachineCategoryVm>> GetCatalogAsync(string culture) =>
        _cache.GetOrCreateAsync<IReadOnlyList<MachineCategoryVm>>($"{IContentCache.Prefix}machines:catalog:{culture}", async () =>
        {
            var categories = await _db.MachineCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Include(c => c.Translations)
                .Include(c => c.Machines.Where(m => m.IsActive).OrderBy(m => m.SortOrder))
                    .ThenInclude(m => m.Translations)
                .AsNoTracking().ToListAsync();

            return categories
                .Where(c => c.Machines.Any())
                .Select(c => new MachineCategoryVm(
                    c.Slug,
                    CategoryName(c, culture),
                    c.Machines.Select(m => Map(m, c, culture)).ToList()))
                .ToList();
        });

    public Task<MachineVm?> GetBySlugAsync(string slug, string culture) =>
        _cache.GetOrCreateAsync<MachineVm?>($"{IContentCache.Prefix}machine:{slug}:{culture}", async () =>
        {
            var m = await _db.Machines
                .Where(x => x.IsActive && x.Slug == slug)
                .Include(x => x.Translations)
                .Include(x => x.Category!).ThenInclude(c => c.Translations)
                .AsNoTracking().FirstOrDefaultAsync();
            return m is null ? null : Map(m, m.Category, culture);
        });

    public Task<IReadOnlyList<MachineVm>> GetFeaturedAsync(string culture, int take = 6) =>
        _cache.GetOrCreateAsync<IReadOnlyList<MachineVm>>($"{IContentCache.Prefix}machines:featured:{culture}:{take}", async () =>
        {
            var machines = await _db.Machines
                .Where(m => m.IsActive && m.IsFeatured)
                .OrderBy(m => m.SortOrder)
                .Take(take)
                .Include(m => m.Translations)
                .Include(m => m.Category!).ThenInclude(c => c.Translations)
                .AsNoTracking().ToListAsync();

            return machines.Select(m => Map(m, m.Category, culture)).ToList();
        });

    private static string CategoryName(MachineCategory? c, string culture)
    {
        if (c is null) return "";
        var t = CultureContext.Pick(c.Translations, x => x.LanguageCode, culture);
        return t?.Name ?? c.Slug;
    }

    private static MachineVm Map(Machine m, MachineCategory? category, string culture)
    {
        var t = CultureContext.Pick(m.Translations, x => x.LanguageCode, culture);
        var name = t?.Name ?? m.Slug;
        return new MachineVm(
            m.Slug,
            m.ImageUrl,
            name,
            t?.ShortDescription ?? "",
            t?.Description ?? "",
            category?.Slug ?? "",
            CategoryName(category, culture),
            m.WorkingHeight,
            m.Capacity,
            m.Reach,
            m.Weight,
            name,
            t?.ShortDescription ?? "");
    }
}
