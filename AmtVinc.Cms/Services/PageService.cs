using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.ViewModels;

namespace AmtVinc.Cms.Services;

public interface IPageService
{
    Task<PageVm?> GetBySlugAsync(string slug, string culture);
}

public class PageService : IPageService
{
    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public PageService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<PageVm?> GetBySlugAsync(string slug, string culture) =>
        _cache.GetOrCreateAsync<PageVm?>($"{IContentCache.Prefix}page:{slug}:{culture}", async () =>
        {
            var page = await _db.Pages
                .Where(p => p.IsActive && p.Slug == slug)
                .Include(p => p.Translations)
                .AsNoTracking().FirstOrDefaultAsync();
            if (page is null) return null;

            var t = CultureContext.Pick(page.Translations, x => x.LanguageCode, culture);
            var title = t?.Title ?? page.Slug;
            return new PageVm(
                page.Slug,
                page.LayoutKey,
                page.CoverImageUrl,
                title,
                t?.Lead ?? "",
                t?.Body ?? "",
                string.IsNullOrWhiteSpace(t?.MetaTitle) ? title : t!.MetaTitle!,
                t?.MetaDescription ?? t?.Lead ?? "");
        });
}
