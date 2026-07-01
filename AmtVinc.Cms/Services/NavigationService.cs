using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.ViewModels;

namespace AmtVinc.Cms.Services;

public interface INavigationService
{
    Task<IReadOnlyList<MenuItemVm>> GetMenuAsync(string culture);
}

public class NavigationService : INavigationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public NavigationService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<MenuItemVm>> GetMenuAsync(string culture) =>
        _cache.GetOrCreateAsync<IReadOnlyList<MenuItemVm>>($"{IContentCache.Prefix}menu:{culture}", async () =>
        {
            var items = await _db.MenuItems
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .Include(m => m.Translations)
                .AsNoTracking().ToListAsync();

            return items.Select(m =>
            {
                var t = CultureContext.Pick(m.Translations, x => x.LanguageCode, culture);
                return new MenuItemVm(t?.Label ?? m.Url, m.Url);
            }).ToList();
        });
}
